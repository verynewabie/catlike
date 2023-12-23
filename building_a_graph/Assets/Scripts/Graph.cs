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
        // ����ResourcesĿ¼�µ�PointԤ����
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
            // �ڶ��������� �Ƿ񱣳�ԭ�������������ѡ����ﲻ��Ҫ
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
            // Time.time����Ϸ��ʼ�����ڵ�ʱ��
            position.y = Mathf.Sin(Mathf.PI * (position.x + Time.time));
            Debug.Log(Time.time);
            point.localPosition = position;
            // ����y���ᵽ��1�������������д�С�ģ�������Щ�ط�����1��ʹ��URPʱ������Graph��ʹ��saturate
        }
    }
}
