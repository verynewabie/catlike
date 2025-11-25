using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
	struct ShadowedDirectionalLight {
		public int visibleLightIndex;
	}
	
	private const string BufferName = "Shadows";
	private readonly CommandBuffer _buffer = new CommandBuffer { name = BufferName };
	private ScriptableRenderContext _context;
	private CullingResults _cullingResults;
	
	private ShadowSettings _settings;
	private int _shadowedDirectionalLightCount;
	private readonly ShadowedDirectionalLight[] _shadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];
	private const int MaxShadowedDirectionalLightCount = 4, MaxCascades = 4;
	
	private static readonly int _dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
	private static readonly int _dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
	private static readonly int _cascadeCountId = Shader.PropertyToID("_CascadeCount");
	private static readonly int _cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
	private static readonly int _shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistance");
	
	// 对于每个片段，我们需要从阴影图集中的相应图块中采样深度信息
	private static readonly Matrix4x4[] _dirShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];
	// 相同级联对应的裁剪球相同，存坐标和半径平方
	private static readonly Vector4[] _cascadeCullingSpheres = new Vector4[MaxCascades];

	public void Setup (ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings) {
		_context = context;
		_cullingResults = cullingResults;
		_settings = settings;
		_shadowedDirectionalLightCount = 0;
	}

	public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
	{
		// 目前只有直线光，所以用visibleLightIndex还是对的
		// GetShadowCasterBounds拿到阴影剔除结果，如果范围内没有投射阴影的物体，或距离超出，就返回false
		if (_shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f
		    && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) {
			_shadowedDirectionalLights[_shadowedDirectionalLightCount] = new ShadowedDirectionalLight { visibleLightIndex = visibleLightIndex };
			return new Vector2(light.shadowStrength, _settings.directional.cascadeCount * _shadowedDirectionalLightCount++);
		}
		// 0传入Light.hlsl后，在Shadows.hlsl中计算时特判返回1
		return Vector2.zero;
	}

	public void Render () {
		if (_shadowedDirectionalLightCount > 0) 
			RenderDirectionalShadows();
		else // 不生成时WebGL会默认生成Default类型，与阴影采样器不兼容
			_buffer.GetTemporaryRT(_dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
	}
	
	public void Cleanup () {
		_buffer.ReleaseTemporaryRT(_dirShadowAtlasId);
		ExecuteBuffer();
	}

	private void RenderDirectionalShadows()
	{
		int atlasSize = (int)_settings.directional.atlasSize;
		// 调用这个方法会走RT池
		_buffer.GetTemporaryRT(_dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
		_buffer.SetRenderTarget(_dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		_buffer.ClearRenderTarget(true, false, Color.clear);
		_buffer.BeginSample(BufferName);
		ExecuteBuffer();
		
		int tiles = _shadowedDirectionalLightCount * _settings.directional.cascadeCount;
		int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
		int tileSize = atlasSize / split;
		for (int i = 0; i < _shadowedDirectionalLightCount; i++) 
			RenderDirectionalShadows(i, split, tileSize);
		
		_buffer.SetGlobalInt(_cascadeCountId, _settings.directional.cascadeCount);
		_buffer.SetGlobalVectorArray(_cascadeCullingSpheresId, _cascadeCullingSpheres);
		_buffer.SetGlobalMatrixArray(_dirShadowMatricesId, _dirShadowMatrices);
		float f = 1f - _settings.directional.cascadeFade;
		_buffer.SetGlobalVector(_shadowDistanceFadeId, new Vector4(1f / _settings.maxDistance, 1f / _settings.distanceFade, 1f / (1f - f * f)));
		_buffer.EndSample(BufferName);
		ExecuteBuffer();
	}
	
	private void ExecuteBuffer () {
		_context.ExecuteCommandBuffer(_buffer);
		_buffer.Clear();
	}
	
	private void RenderDirectionalShadows (int index, int split, int tileSize) {
		ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
		// 平行光是无限远光源，使用正交投影
		var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.visibleLightIndex, BatchCullingProjectionType.Orthographic);
		int cascadeCount = _settings.directional.cascadeCount;
		int tileOffset = index * cascadeCount;
		Vector3 ratios = _settings.directional.CascadeRatios;
		for (int i = 0; i < cascadeCount; i++)
		{
			_cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
				light.visibleLightIndex, i, cascadeCount, ratios, tileSize, 0f,
				out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
				out ShadowSplitData splitData
			);
			if (index == 0)
			{
				Vector4 cullingSphere = splitData.cullingSphere;
				cullingSphere.w *= cullingSphere.w;
				_cascadeCullingSpheres[i] = cullingSphere;
			}
			// 包含级联阴影映射(Cascaded Shadow Maps)的裁剪信息
			shadowSettings.splitData = splitData;
			int tileIndex = tileOffset + i;
			_dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, 
				SetTileViewport(tileIndex, split, tileSize), split);
			_buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
			ExecuteBuffer();
			// 调用ShadowCaster Pass
			_context.DrawShadows(ref shadowSettings);
		}
	}
	
	private Vector2 SetTileViewport (int index, int split, float tileSize) {
		Vector2 offset = new Vector2(index % split, index / split);
		_buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
		return offset;
	}
	
	// 正交投影无需考虑透视除法
	private Matrix4x4 ConvertToAtlasMatrix (Matrix4x4 m, Vector2 offset, int split) {
		// 投影矩阵第三行设计Far和Near，因此最后的结果都在第三行，取相反数即可
		if (SystemInfo.usesReversedZBuffer) {
			m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
		}
		// 相当于先左乘[0.5,0,0,0.5][0,0.5,0,0.5][0,0,0.5,0.5][0,0,0,1]矩阵，把-1~1映射到0~1
		// 再左乘[s,0,0,o.x*s][0,s,0,o.y*s][0,0,1,0][0,0,0,1]，s代表scale，o代表offset
		float scale = 1f / split;
		m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
		m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);
		return m;
	}
}
