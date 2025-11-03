using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
	private static readonly int _dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
	private static readonly int _dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
	private const string BufferName = "Lighting";

	private readonly CommandBuffer _buffer = new CommandBuffer {
		name = BufferName
	};
	
	public void Setup(ScriptableRenderContext context) {
		_buffer.BeginSample(BufferName);
		SetupDirectionalLight();
		_buffer.EndSample(BufferName);
		context.ExecuteCommandBuffer(_buffer);
		_buffer.Clear();
	}

	void SetupDirectionalLight()
	{
		// 获取场景中最重要的直线光，可通过Window/Rendering/Lighting/Environment中Sun Source字段配置
		Light light = RenderSettings.sun;
		// Color(RGBA)和V3到V4有隐式转换，buffer中定义的是float3，Frame Debugger中看到_DirectionalLightColor和_DirectionalLightDirection传的都是V4，忽略第4维当做V3用
		_buffer.SetGlobalVector(_dirLightColorId, light.color.linear * light.intensity);
		_buffer.SetGlobalVector(_dirLightDirectionId, -light.transform.forward);
	}
}
