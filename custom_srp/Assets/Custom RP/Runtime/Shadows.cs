using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
	struct ShadowedDirectionalLight {
		public int visibleLightIndex;
		public float slopeScaleBias;
		public float nearPlaneOffset;
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
	private static readonly int _shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
	private static readonly int _shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
	private static readonly int _cascadeDataId = Shader.PropertyToID("_CascadeData");
	
	// 对于每个片段，我们需要从阴影图集中的相应图块中采样深度信息
	private static readonly Matrix4x4[] _dirShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades];
	// 相同级联对应的裁剪球相同，存坐标和半径平方
	private static readonly Vector4[] _cascadeCullingSpheres = new Vector4[MaxCascades];
	// x:1/裁剪球半径 y:texelSize*1.4142136
	private static readonly Vector4[] _cascadeData = new Vector4[MaxCascades];
	
	private static readonly string[] _directionalFilterKeywords = {
		"_DIRECTIONAL_PCF3",
		"_DIRECTIONAL_PCF5",
		"_DIRECTIONAL_PCF7",
	};
	
	private static readonly string[] _cascadeBlendKeywords = {
		"_CASCADE_BLEND_SOFT",
		"_CASCADE_BLEND_DITHER"
	};

	public void Setup (ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings) {
		_context = context;
		_cullingResults = cullingResults;
		_settings = settings;
		_shadowedDirectionalLightCount = 0;
	}

	public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
	{
		// 目前只有直线光，所以用visibleLightIndex还是对的
		// GetShadowCasterBounds拿到阴影剔除结果，如果范围内没有投射阴影的物体，或距离超出，就返回false
		if (_shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f
		    && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) {
			_shadowedDirectionalLights[_shadowedDirectionalLightCount] = new ShadowedDirectionalLight
			{
				visibleLightIndex = visibleLightIndex,
				slopeScaleBias = light.shadowBias,
				nearPlaneOffset = light.shadowNearPlane
			};
			// 我们只是利用 light.shadowNormalBias 和 light.shadowBias 属性，它们原来的功能和我们的用法不一样
			return new Vector3(light.shadowStrength, _settings.directional.cascadeCount * _shadowedDirectionalLightCount++, light.shadowNormalBias);
		}
		// 0传入Light.hlsl后，在Shadows.hlsl中计算时特判返回1
		return Vector3.zero;
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
		_buffer.SetGlobalVectorArray(_cascadeDataId, _cascadeData);
		_buffer.SetGlobalMatrixArray(_dirShadowMatricesId, _dirShadowMatrices);
		float f = 1f - _settings.directional.cascadeFade;
		_buffer.SetGlobalVector(_shadowDistanceFadeId, new Vector4(1f / _settings.maxDistance, 1f / _settings.distanceFade, 1f / (1f - f * f)));
		SetKeywords(_directionalFilterKeywords, (int)_settings.directional.filter - 1);
		SetKeywords(_cascadeBlendKeywords, (int)_settings.directional.cascadeBlend - 1);
		_buffer.SetGlobalVector(_shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
		_buffer.EndSample(BufferName);
		ExecuteBuffer();
	}
	
	private void SetKeywords (string[] keywords, int enabledIndex) {
		for (int i = 0; i < keywords.Length; i++) {
			if (i == enabledIndex) {
				_buffer.EnableShaderKeyword(keywords[i]);
			}
			else {
				_buffer.DisableShaderKeyword(keywords[i]);
			}
		}
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
		float cullingFactor = Mathf.Max(0f, 0.8f - _settings.directional.cascadeFade);	
		for (int i = 0; i < cascadeCount; i++)
		{
			_cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
				light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
				out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
				out ShadowSplitData splitData
			);
			if (index == 0)
			{
				SetCascadeData(i, splitData.cullingSphere, tileSize);
			}
			// 包含级联阴影映射(Cascaded Shadow Maps)的裁剪信息
			splitData.shadowCascadeBlendCullingFactor = cullingFactor;
			shadowSettings.splitData = splitData;
			int tileIndex = tileOffset + i;
			_dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, 
				SetTileViewport(tileIndex, split, tileSize), split);
			_buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
			_buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
			ExecuteBuffer();
			// 调用ShadowCaster Pass
			_context.DrawShadows(ref shadowSettings);
			_buffer.SetGlobalDepthBias(0f, 0f);
		}
	}
	
	private void SetCascadeData (int index, Vector4 cullingSphere, float tileSize) {
		// 每个像素代表的世界空间大小
		float texelSize = 2f * cullingSphere.w / tileSize;
		float filterSize = texelSize * ((float)_settings.directional.filter + 1f);
		// 防止采样位置超出 Shadow Map
		cullingSphere.w -= filterSize;
		cullingSphere.w *= cullingSphere.w;
		_cascadeCullingSpheres[index] = cullingSphere;
		_cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
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
