using Unity.Mathematics;
using UnityEngine;
using static FunctionLibrary;

public class GPUGraph : MonoBehaviour
{
    const int maxResolution = 200;
    [SerializeField]
    Material material;

    [SerializeField]
    Mesh mesh;
    [SerializeField]
    ComputeShader computeShader;
    ComputeBuffer positionsBuffer;
    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        timeId = Shader.PropertyToID("_Time"),
        transitionProgressId = Shader.PropertyToID("_TransitionProgress");
    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution;
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);
        if (transitioning)
        {
            computeShader.SetFloat(
                transitionProgressId,
                Mathf.SmoothStep(0f, 1f, duration / transitionDuration)
            );
        }
        var kernelIndex =
            (int)function + (int)(transitioning ? transitionFunction : function) * FunctionCount;
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);

        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);
        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);
        // ����Ϊ0 0 0���߳�һ����������
        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        // 0��sub-material��index�����ﲻ��Ҫ
        Graphics.DrawMeshInstancedProcedural(
            mesh, 0, material, bounds, resolution * resolution
        );
    }
    [SerializeField]
    [Range(10, maxResolution)]
    int resolution = 100;

    [SerializeField]
    FunctionName function = FunctionName.Torus;

    [SerializeField, Min(0f)]
    float functionDuration = 1f, transitionDuration = 1f;


    public enum TransitionMode { Cycle, Random }

    [SerializeField]
    TransitionMode transitionMode = TransitionMode.Random;
    float duration = 0f;
    bool transitioning = false;

    FunctionName transitionFunction;
    
    void OnEnable()
    {
        // Awake���ܶ����Ƿ�enable������ã����enable�����OnEnable
        // �����ص�ʱ�����disable��Ϸ������enable
        // �ڶ���������һ������Ĵ�С����һ�������Ƕ������
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4);
    }
    void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }
    // play mode�¸���resolution��Ŀ��Ч����update�������������������Ŀ
    void Update()
    {
        duration += Time.deltaTime;
        if (transitioning)
        {
            if (duration >= transitionDuration)
            {
                duration -= transitionDuration;
                transitioning = false;
            }
        }
        else if (duration >= functionDuration)
        {
            duration -= functionDuration;
            transitioning = true;
            transitionFunction = function;
            PickNextFunction();
        }
        UpdateFunctionOnGPU();
    }
    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            GetNextFunctionName(function) :
            GetRandomFunctionNameOtherThan(function);
    }
}
