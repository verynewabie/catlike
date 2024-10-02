using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    private Transform hoursPivot;
    private Transform minutesPivot;
    private Transform secondsPivot; 
    const float hoursToDegrees = -30f;
    const float minutesToDegrees = -6f;
    const float secondsToDegrees = -6f;
    private void Awake()
    {
        hoursPivot = this.transform.Find("Hours Arm Pivot").GetComponent<Transform>();
        minutesPivot = this.transform.Find("Minutes Arm Pivot").GetComponent<Transform>();
        secondsPivot = this.transform.Find("Seconds Arm Pivot").GetComponent<Transform>();
    }
    void Update()
    {
        DateTime time = DateTime.Now;
        hoursPivot.localRotation =
            Quaternion.Euler(0f, 0f, hoursToDegrees * time.Hour);
        minutesPivot.localRotation =
            Quaternion.Euler(0f, 0f, minutesToDegrees * time.Minute);
        secondsPivot.localRotation =
            Quaternion.Euler(0f, 0f, secondsToDegrees * time.Second);
    }
}
