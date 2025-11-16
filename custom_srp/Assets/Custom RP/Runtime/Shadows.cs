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
	private const int MaxShadowedDirectionalLightCount = 4;
	
	private static readonly int _dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

	public void Setup (ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings) {
		_context = context;
		_cullingResults = cullingResults;
		_settings = settings;
		_shadowedDirectionalLightCount = 0;
	}

	public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
	{
		// 目前只有直线光，所以用visibleLightIndex还是对的
		// GetShadowCasterBounds拿到阴影剔除结果，如果范围内没有投射阴影的物体，或距离超出，就返回false
		if (_shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount && light.shadows != LightShadows.None && light.shadowStrength > 0f
		    && _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) {
			_shadowedDirectionalLights[_shadowedDirectionalLightCount++] = new ShadowedDirectionalLight {
				visibleLightIndex = visibleLightIndex
			};
		}
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
		
		int split = _shadowedDirectionalLightCount <= 1 ? 1 : 2;
		int tileSize = atlasSize / split;
		for (int i = 0; i < _shadowedDirectionalLightCount; i++) {
			RenderDirectionalShadows(i, split, tileSize);
		}
		
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
		_cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
			light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
			out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
			out ShadowSplitData splitData
		);
		// 包含级联阴影映射(Cascaded Shadow Maps)的裁剪信息
		shadowSettings.splitData = splitData;
		SetTileViewport(index, split, tileSize);
		_buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
		ExecuteBuffer();
		// 调用ShadowCaster Pass
		_context.DrawShadows(ref shadowSettings);
	}
	
	private void SetTileViewport (int index, int split, float tileSize) {
		Vector2 offset = new Vector2(index % split, index / split);
		_buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
	}
}
