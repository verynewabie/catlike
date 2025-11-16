using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
	private const string BufferName = "Render Camera";

	// 天空盒渲染有专门函数，没有的就用CommandBuffer
	private readonly CommandBuffer _buffer = new CommandBuffer { name = BufferName };

	private static readonly ShaderTagId _unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
	private static readonly ShaderTagId _litShaderTagId = new ShaderTagId("CustomLit");
	
	private ScriptableRenderContext _context;

	private Camera _camera;
	private CullingResults _cullingResults;
	private readonly Lighting _lighting = new Lighting();
	
	public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
	{
		_context = context;
		_camera = camera;
		
		// 当有两个相机时，要更新buffer的名字，不然在Frame Debugger中会合并
		PrepareBuffer();
		// 必须在剔除前执行，这样剔除才正确
		PrepareForSceneWindow();
		if (!Cull(shadowSettings.maxDistance)) {
			return;
		}
		
		_buffer.BeginSample(SampleName);
		ExecuteBuffer();
		_lighting.Setup(context, _cullingResults, shadowSettings);
		_buffer.EndSample(SampleName);
		// Lighting的Setup会设置光源的View、Projection，而CameraRender的Setup会设回Camera的
		Setup();
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		DrawUnsupportedShaders();
		DrawGizmos();
		_lighting.Cleanup();
		Submit();
	}
	
	private void DrawVisibleGeometry (bool useDynamicBatching, bool useGPUInstancing) {
		var sortingSettings = new SortingSettings(_camera) {
			criteria = SortingCriteria.CommonOpaque
		};
		// Unity只会渲染那些在Tags中指定LightMode为_unlitShaderTagId的，不过Unlit的Color和Transparent都没有LightMode，可能是SRP的源码发力了：没有Pass就默认为SRPDefaultUnlit
		var drawingSettings = new DrawingSettings(_unlitShaderTagId, sortingSettings) { enableDynamicBatching = useDynamicBatching, enableInstancing = useGPUInstancing };
		drawingSettings.SetShaderPassName(1, _litShaderTagId);
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
		_context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
		
		_context.DrawSkybox(_camera);
		
		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;
		_context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
	}
	
	private void Submit () {
		_buffer.EndSample(SampleName);
		ExecuteBuffer();
		_context.Submit();
	}
	
	private void Setup () {
		// 设置unity_MatrixVP和一些其它属性
		_context.SetupCameraProperties(_camera);
		// Skybox = 1, Color, Depth, Nothing = 4，只有Skybox时相机才会渲染天空盒
		CameraClearFlags flags = _camera.clearFlags;
		_buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear);
		_buffer.BeginSample(SampleName);
		ExecuteBuffer(); 
	}
	
	/// <summary>
	/// 执行Command并清空Buffer
	/// </summary>
	private void ExecuteBuffer () {
		// 提交命令，绘制是在_context.Submit()之后
		_context.ExecuteCommandBuffer(_buffer);
		_buffer.Clear();
	}
	
	private bool Cull (float maxDistance) {
		if (_camera.TryGetCullingParameters(out ScriptableCullingParameters p)) {
			p.shadowDistance = Mathf.Min(maxDistance, _camera.farClipPlane);
			// 这里传ref仅仅为了优化
			_cullingResults = _context.Cull(ref p);
			return true;
		}
		return false;
	}
	
}
