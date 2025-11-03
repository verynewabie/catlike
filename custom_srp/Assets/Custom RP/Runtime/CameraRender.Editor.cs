using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

public partial class CameraRenderer
{
	// 这些是声明，如果构建中没有实现，就会裁剪掉调用，如果这些方法有访问修饰符，就必须有实现
	partial void PrepareBuffer();
	partial void DrawUnsupportedShaders();
	partial void DrawGizmos ();
	partial void PrepareForSceneWindow ();
#if UNITY_EDITOR
	private static Material _errorMaterial;
	// 覆盖Unity default Shader的Pass
	private static readonly ShaderTagId[] _legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	 partial void PrepareForSceneWindow () {
		if (_camera.cameraType == CameraType.SceneView) {
			// Emits UI geometry into the Scene view for rendering
			ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
		}
	}
	
	// ReSharper disable once MemberCanBePrivate.Global
	public string SampleName { get; set; }
	
	partial void PrepareBuffer ()
	{
		Profiler.BeginSample("Editor Only");
		// 访问_camera.name会有内存分配，在两个相机名字分别为Main Camera和Secondary Camera的情况下每帧分配98B
		_buffer.name = SampleName = _camera.name;
		Profiler.EndSample();
	}
	
	partial void DrawGizmos () {
		// 我们现在还没有ImageEffects，所以直接两种都调用即可，这里是绘制Scene下的Gizmos
		if (Handles.ShouldRenderGizmos()) {
			_context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
			_context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
		}
	}
	
	partial void DrawUnsupportedShaders()
	{
		if (_errorMaterial == null)
		{
			_errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
		}

		var drawingSettings = new DrawingSettings(_legacyShaderTagIds[0], new SortingSettings(_camera))
		{
			overrideMaterial = _errorMaterial
		};
		for (int i = 1; i < _legacyShaderTagIds.Length; i++)
		{
			drawingSettings.SetShaderPassName(i, _legacyShaderTagIds[i]);
		}

		var filteringSettings = FilteringSettings.defaultValue;
		_context.DrawRenderers(
			_cullingResults, ref drawingSettings, ref filteringSettings
		);
	}
	
#else
	// Editor下没有访问_camera.name, 每帧分配的内容也为0B
	public const string SampleName = BufferName;
#endif
}
