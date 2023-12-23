using System;
using UnityEngine;
public class Graph : MonoBehaviour
{
    private Transform pointPrefab;


    [SerializeField]
    [Range(10,100)]
    int resolution = 10;

    // Arrays are objects, not simple values
    Transform[] points;
    void Awake()
    {
        // 加载Resources目录下的Point预制体
        pointPrefab = Resources.Load<Transform>("Point");
        float step = 2f / resolution;
        Vector3 scale = Vector3.one * step;
        Vector3 position = Vector3.zero;
        points = new Transform[resolution];
        for (int i = 0; i < resolution; i++)
        {
            Transform point = Instantiate(pointPrefab);
            points[i]= point;
            position.x = (i + 0.5f) * step - 1f;
            point.localPosition = position;
            point.localScale = scale;
            // 第二个参数是 是否保持原来的世界坐标的选项，这里不需要
            point.SetParent(transform,false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < points.Length; i++)
        {
            Transform point = points[i];
            Vector3 position = point.localPosition;
            // Time.time是游戏开始到现在的时间
            position.y = Mathf.Sin(Mathf.PI * (position.x + Time.time));
            Debug.Log(Time.time);
            point.localPosition = position;
            // 这里y最大会到达1，而立方体是有大小的，导致有些地方超过1，使用URP时可以在Graph中使用saturate
        }
    }
}
