using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
	private readonly bool _useDynamicBatching;
	private readonly bool _useGPUInstancing;
	
	private readonly CameraRenderer _renderer = new CameraRenderer();
	// 由List.ToArray传参，每帧都会分配内存，虽然分配后的内存只复制引用
	protected override void Render(ScriptableRenderContext context, Camera[] cameras)
	{
		foreach (var camera in cameras)
		{
			_renderer.Render(context, camera, _useDynamicBatching, _useGPUInstancing);
		}
	}

	protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
	{
		foreach (var camera in cameras)
		{
			_renderer.Render(context, camera, _useDynamicBatching, _useGPUInstancing);
		}
	}
	
	public CustomRenderPipeline (bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher) 
	{
		GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
		_useDynamicBatching = useDynamicBatching;
		_useGPUInstancing = useGPUInstancing;
		GraphicsSettings.lightsUseLinearIntensity = true;
	}
}
