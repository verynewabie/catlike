using UnityEngine;

public class MeshBall : MonoBehaviour {

	private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
	private static readonly int _metallicId = Shader.PropertyToID("_Metallic");
	private static readonly int _smoothnessId = Shader.PropertyToID("_Smoothness");

	[SerializeField]
	private Mesh _mesh = null;

	// 要用支持GPU Instancing的材质
	[SerializeField]
	private Material _material = null;
	// 一次最多可以提供 1023 个实例
	private readonly Matrix4x4[] _matrices = new Matrix4x4[1023];
	private readonly Vector4[] _baseColors = new Vector4[1023];
	private readonly float[] _metallic = new float[1023];
	private readonly float[] _smoothness = new float[1023];
	private MaterialPropertyBlock _block;
	
	private void Awake () {
		for (int i = 0; i < _matrices.Length; i++) {
			_matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f,
				Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f),
				Vector3.one * Random.Range(0.5f, 1.5f));
			_baseColors[i] = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1f));
			_metallic[i] = Random.value < 0.25f ? 1f : 0f;
			_smoothness[i] = Random.Range(0.05f, 0.95f);
		}
	}
	
	private void Update () {
		if (_block == null) {
			_block = new MaterialPropertyBlock();
			_block.SetVectorArray(_baseColorId, _baseColors);
			_block.SetFloatArray(_metallicId, _metallic);
			_block.SetFloatArray(_smoothnessId, _smoothness);
		}
		Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, 1023, _block);
	}
	
}
