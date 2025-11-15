using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI 
{
	private MaterialEditor _editor;
	private Object[] _materials;
	private MaterialProperty[] _properties;
	private bool _showPresets;
	
	public bool Clipping {
		set => SetProperty("_Clipping", "_CLIPPING", value);
	}

	public bool PremultiplyAlpha {
		set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
	}

	public BlendMode SrcBlend {
		set => SetProperty("_SrcBlend", (float)value);
	}

	public BlendMode DstBlend {
		set => SetProperty("_DstBlend", (float)value);
	}

	public bool ZWrite {
		set => SetProperty("_ZWrite", value ? 1f : 0f);
	}
	
	public bool HasProperty (string name) => FindProperty(name, _properties, false) != null;
	
	public bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");
	
	RenderQueue RenderQueue {
		set {
			foreach (var o in _materials)
			{
				var m = (Material)o;
				m.renderQueue = (int)value;
			}
		}
	}

	public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] properties) {
		base.OnGUI(materialEditor, properties);
		_editor = materialEditor;
		_materials = materialEditor.targets;
		_properties = properties;
		
		EditorGUILayout.Space();
		// 第三个参数：是否允许点击标签区域来切换折叠状态
		_showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
		if (_showPresets) {
			OpaquePreset();
			ClipPreset();
			FadePreset();
			TransparentPreset();
		}
	}
	
	private bool SetProperty (string name, float value) {
		// 第三个参数：找不到是否抛异常
		MaterialProperty property = FindProperty(name, _properties, false);
		if (property != null) {
			property.floatValue = value;
			return true;
		}
		return false;
	}
	
	private void SetProperty (string name, string keyword, bool value) 
	{
		if (SetProperty(name, value ? 1f : 0f)) {
			SetKeyword(keyword, value);
		}
	}
	
	private void SetKeyword (string keyword, bool enabled)
	{
		if (enabled) 
		{
			foreach (var o in _materials)
			{
				var m = (Material)o;
				m.EnableKeyword(keyword);
			}
		}
		else 
		{
			foreach (var o in _materials)
			{
				var m = (Material)o;
				m.DisableKeyword(keyword);
			}
		}
	}
	
	private void OpaquePreset () {
		if (PresetButton("Opaque")) {
			Clipping = false;
			PremultiplyAlpha = false;
			SrcBlend = BlendMode.One;
			DstBlend = BlendMode.Zero;
			ZWrite = true;
			RenderQueue = RenderQueue.Geometry;
		}
	}
	
	private void ClipPreset () {
		if (PresetButton("Clip")) {
			Clipping = true;
			PremultiplyAlpha = false;
			SrcBlend = BlendMode.One;
			DstBlend = BlendMode.Zero;
			ZWrite = true;
			RenderQueue = RenderQueue.AlphaTest;
		}
	}
	
	private void FadePreset () {
		if (PresetButton("Fade")) {
			Clipping = false;
			PremultiplyAlpha = false;
			SrcBlend = BlendMode.SrcAlpha;
			DstBlend = BlendMode.OneMinusSrcAlpha;
			ZWrite = false;
			RenderQueue = RenderQueue.Transparent;
		}
	}
	
	private void TransparentPreset () {
		if (HasPremultiplyAlpha && PresetButton("Transparent")) {
			Clipping = false;
			PremultiplyAlpha = true;
			SrcBlend = BlendMode.One;
			DstBlend = BlendMode.OneMinusSrcAlpha;
			ZWrite = false;
			RenderQueue = RenderQueue.Transparent;
		}
	}
	
	private bool PresetButton (string name) {
		if (GUILayout.Button(name)) {
			_editor.RegisterPropertyChangeUndo(name);
			return true;
		}
		return false;
	}
}
