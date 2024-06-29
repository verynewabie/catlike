// TODO 3.4 shaders
using UnityEngine;

public class DrawingFractal : MonoBehaviour
{
    struct FractalPart
    {
        public Vector3 direction,worldPosition;
        // 一直用四元数累乘，浮点误差积累，可能造成四元数模长不为1，导致报错
        public Quaternion rotation,worldRotation;
        // 所以再开一个变量记录角度
        public float spinAngle;
    }
    FractalPart[][] parts;
    private Matrix4x4[][] matrixs;
    
    [SerializeField]
    public Mesh mesh;
    [SerializeField, Range(1, 8)]
    public int depth = 4;
    [SerializeField]
    public Material material;
    static Vector3[] directions = {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    static Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };
    
    ComputeBuffer[] matrixsBuffers;
    private void OnEnable()
    {
        parts = new FractalPart[depth][];
        matrixs = new Matrix4x4[depth][];
        matrixsBuffers = new ComputeBuffer[depth];
        int stride = 16 * 4;
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            parts[i] = new FractalPart[length];
            matrixs[i] = new Matrix4x4[length];
            matrixsBuffers[i] = new ComputeBuffer(length, stride);
        }
        parts[0][0] = CreatePart(0);
        for (int li = 1; li < parts.Length; li++)
        {
            FractalPart[] levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                for (int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < matrixsBuffers.Length; i++) {
            matrixsBuffers[i].Release();
        }
        parts = null;
        matrixs = null;
        matrixsBuffers = null;
    }

    // 在Inspector中修改值后调用 
    private void OnValidate()
    {
        if (parts != null && enabled)
        {
            Debug.Log("OnValidate");
            OnDisable();
            OnEnable();
        }
        
    }

    FractalPart CreatePart(int childIndex)
    {
        return new FractalPart // still allocated on stack
        {
            direction = directions[childIndex],
            rotation = rotations[childIndex],
        };
    }

    void Update()
    {
        float spinAngleDelta = 22.5f * Time.deltaTime;
        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle += spinAngleDelta;
        rootPart.worldRotation =
            rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f);
        parts[0][0] = rootPart;
        matrixs[0][0] = Matrix4x4.TRS(rootPart.worldPosition
            , rootPart.worldRotation, Vector3.one);
        
        float scale = 1f;
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            FractalPart[] parentParts = parts[li - 1];
            FractalPart[] levelParts = parts[li];
            Matrix4x4[] levelMatrixs = matrixs[li];
            
            for (int fpi = 0; fpi < levelParts.Length; fpi++)
            {
                FractalPart parent = parentParts[fpi / 5];
                FractalPart part = levelParts[fpi];
                part.spinAngle += spinAngleDelta;
                part.worldRotation =
                    parent.worldRotation *
                    (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
                part.worldPosition =
                    parent.worldPosition +
                    parent.worldRotation * (1.5f * scale * part.direction);
                levelParts[fpi] = part;
                levelMatrixs[fpi] = Matrix4x4.TRS(part.worldPosition,
                    part.worldRotation,
                    Vector3.one * scale);
            }
        }

        int num = 0;
        foreach (ComputeBuffer buffer in matrixsBuffers)
        {
            buffer.SetData(matrixs[num++]);
        }
    }
}
