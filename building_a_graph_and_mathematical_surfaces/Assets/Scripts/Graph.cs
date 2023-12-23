using System;
using UnityEngine;
using static UnityEngine.Time;
using static FunctionLibrary;
using static UnityEngine.Mathf; // 所有static和constant都能使用，只能用于class
public class Graph : MonoBehaviour
{
    private Transform pointPrefab;


    [SerializeField]
    [Range(10,100)]
    int resolution = 10;

    [SerializeField]
    FunctionLibrary.FunctionName function;

    // Arrays are objects, not simple values
    Transform[] points;
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

    // play mode下更改resolution数目有效但是update函数并不依赖于这个数目
    void Update()
    {
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);
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
}
