using TMPro;
using UnityEngine;

public class FrameRateCounter : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI display;
    [SerializeField, Range(0.1f, 2f)]
    float sampleDuration = 1f; // 1s更新一次帧率
    float bestDuration = float.MaxValue;
    float worstDuration = 0f;
    // 显示的帧率是update的调用频率，与真正的频率并不完全一样
    public enum DisplayMode{FPS,MS };
    [SerializeField]
    DisplayMode displayMode = DisplayMode.FPS;
    void Start()
    {
        
    }

    int frames;

    float duration;

    void Update()
    {
        float frameDuration = Time.unscaledDeltaTime;
        frames += 1;
        duration += frameDuration;
        bestDuration = Mathf.Min(bestDuration, frameDuration);
        worstDuration = Mathf.Max(worstDuration, frameDuration);
        if (duration >= sampleDuration)
        {
            if (displayMode == DisplayMode.FPS)
            {
                display.SetText(
                    "FPS\n{0:0}\n{1:0}\n{2:0}",
                    1f / bestDuration,
                    frames / duration,
                    1f / worstDuration
                );
            }
            else
            {
                display.SetText(
                    "MS\n{0:0}\n{1:0}\n{2:0}",
                    1000f * bestDuration,
                    1000f * duration / frames,
                    1000f * worstDuration
                );
            }
            frames = 0;
            duration = 0f;
            bestDuration = float.MaxValue;
            worstDuration = 0f;
        }
    }
}
