using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
	private const string BufferName = "Lighting";
	// 最多支持四个直线光
	private const int MaxDirLightCount = 4;

	// 这里不用StructuredBuffer，是因为支持不好，Computer Shader好一些
	private static readonly int _dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
	private static readonly int	_dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
	private static readonly int	_dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
	private static readonly int _dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
	private static readonly Vector4[] _dirLightColors = new Vector4[MaxDirLightCount];
	private static readonly Vector4[] _dirLightDirections = new Vector4[MaxDirLightCount];
	private static readonly Vector4[] _dirLightShadowData = new Vector4[MaxDirLightCount];
	
	private CullingResults _cullingResults;
	private readonly CommandBuffer _buffer = new CommandBuffer { name = BufferName };
	private readonly Shadows _shadows = new Shadows();
	
	public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings) {
		_cullingResults = cullingResults;
		_buffer.BeginSample(BufferName);
		_shadows.Setup(context, cullingResults, shadowSettings);
		SetupLights();
		_shadows.Render();
		_buffer.EndSample(BufferName);
		context.ExecuteCommandBuffer(_buffer);
		_buffer.Clear();
	}
	
	public void Cleanup () {
		_shadows.Cleanup();
	}

	private void SetupLights()
	{
		// A struct provides a connection to a native memory buffer
		NativeArray<VisibleLight> visibleLights = _cullingResults.visibleLights;
		
		int dirLightCount = 0;
		for (int i = 0; i < visibleLights.Length; i++)
		{
			var visibleLight = visibleLights[i];
			if (visibleLight.lightType != LightType.Directional) continue;
			// TODO：我们现在从CullResult中拿光，传Index，再根据Index调用CullResult的计算方法，当不止有直线光时这里会报错
			SetupDirectionalLight(dirLightCount++, ref visibleLight);
			if (dirLightCount >= MaxDirLightCount)
				break;
		}

		_buffer.SetGlobalInt(_dirLightCountId, dirLightCount);
		_buffer.SetGlobalVectorArray(_dirLightColorsId, _dirLightColors);
		_buffer.SetGlobalVectorArray(_dirLightDirectionsId, _dirLightDirections);
		_buffer.SetGlobalVectorArray(_dirLightShadowDataId, _dirLightShadowData);
	}

	private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
	{
		// finalColor是color乘上intensity
		_dirLightColors[index] = visibleLight.finalColor;
		// 光没有缩放的话，可以这样拿方向
		_dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
		_dirLightShadowData[index] = _shadows.ReserveDirectionalShadows(visibleLight.light, index);
	}
}
