using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    private ShadowSettings _shadows;
    // 修改这些选项会立即生效，因为Unity检测到修改会重新创建实例
    [SerializeField]
    private bool _useDynamicBatching = false, _useGPUInstancing = false, _useSRPBatcher = false;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(_useDynamicBatching, _useGPUInstancing, _useSRPBatcher, _shadows);
    }
}
