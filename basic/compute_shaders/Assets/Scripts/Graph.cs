using System;
using UnityEngine;
using static FunctionLibrary;
public class Graph : MonoBehaviour
{
    private Transform pointPrefab;


    [SerializeField]
    [Range(10,200)]
    int resolution = 100;

    [SerializeField]
    FunctionName function = FunctionName.Torus;

    [SerializeField, Min(0f)]
    float functionDuration = 1f,transitionDuration = 1f;

    // Arrays are objects, not simple values
    Transform[] points;

    public enum TransitionMode { Cycle, Random }

    [SerializeField]
    TransitionMode transitionMode = TransitionMode.Random;
    void Awake()
    {
        // 加载Resources目录下的Point预制体
        pointPrefab = Resources.Load<Transform>("Point");
        float step = 2f / resolution;
        Vector3 scale = Vector3.one * step;
        points = new Transform[resolution*resolution];
        for (int i = 0; i < points.Length; i++)
        {
            Transform point = points[i] = Instantiate(pointPrefab);
            point.localScale = scale;
            point.SetParent(transform, false);
        }
    }
    float duration = 0f;
    bool transitioning = false;

    FunctionName transitionFunction;
    // play mode下更改resolution数目有效但是update函数并不依赖于这个数目
    void Update()
    {
        duration += Time.deltaTime;
        if (transitioning) {
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
        if (transitioning)
        {
            UpdateFunctionTransition();
        }
        else
        {
            UpdateFunction();
        }
    }
    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            GetNextFunctionName(function) :
            GetRandomFunctionNameOtherThan(function);
    }
    void UpdateFunction()
    {
        Function f = GetFunction(function);
        float time = Time.time;
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }
            float u = (x + 0.5f) * step - 1f;
            points[i].localPosition = f(u, v, time);
        }
    }

    void UpdateFunctionTransition()
    {
        Function from = GetFunction(transitionFunction);
        Function to = GetFunction(function);
        float progress = duration / transitionDuration;
        float time = Time.time;
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }
            float u = (x + 0.5f) * step - 1f;
            points[i].localPosition = FunctionLibrary.Morph(
                u, v, time, from, to, progress
            );
        }
    }

}
