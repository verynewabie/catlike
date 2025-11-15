using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour {
	
	private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
	private static readonly int _cutoffId = Shader.PropertyToID("_Cutoff");
	private static readonly int _metallicId = Shader.PropertyToID("_Metallic");
	private static readonly int _smoothnessId = Shader.PropertyToID("_Smoothness");
	
	private static MaterialPropertyBlock _block;
	
	[SerializeField]
	private Color _baseColor = Color.white;
	[SerializeField, Range(0f, 1f)]
	private float _cutoff = 0.5f, _metallic = 0f, _smoothness = 0.5f;
	
	// 主要在脚本被加载或 Inspector 中的值被修改时，仅在编辑器下被调用，早于Awake
	private void OnValidate () {
		_block ??= new MaterialPropertyBlock();
		_block.SetColor(_baseColorId, _baseColor);
		_block.SetFloat(_cutoffId, _cutoff);
		_block.SetFloat(_metallicId, _metallic);
		_block.SetFloat(_smoothnessId, _smoothness);
		GetComponent<Renderer>().SetPropertyBlock(_block);
	}

	private void Awake()
	{
		OnValidate();
	}
	
}
