#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#include "Surface.hlsl"

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

// 更清晰，尽管这对于我们支持的平台来说并无区别
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
// 阴影贴图采样方式，因为常规的双线性过滤对于深度数据而言并不适用
// 采样器状态可以通过在其名称中包含特定关键词来内联定义：linear 使用线性过滤，clamp 使用clamp寻址模式，compare 启用深度比较功能
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
	int _CascadeCount;
	float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
	float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
	// 1/maxDistance 1/distanceFade 1/(1-cascadeFade*cascadeFade)
	float4 _ShadowDistanceFade;
CBUFFER_END

struct DirectionalShadowData {
	float strength;
	int tileIndex;
};

struct ShadowData {
	int cascadeIndex;
	float strength;
};

float FadedShadowStrength (float distance, float scale, float fade) {
	return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData (Surface surfaceWS) {
	ShadowData data;
	data.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
	int i;
	for (i = 0; i < _CascadeCount; i++) {
		float4 sphere = _CascadeCullingSpheres[i];
		float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
		if (distanceSqr < sphere.w) {
			// 最后一级分辨率低且会突然消失
			if (i == _CascadeCount - 1) {
				// 这里是一个平滑且接近线性的Fade（与d/r接近线性）
				data.strength *= FadedShadowStrength(distanceSqr, 1.0 / sphere.w, _ShadowDistanceFade.z);
			}
			break;
		}
	}

	if (i == _CascadeCount) {
		data.strength = 0.0;
	}
	data.cascadeIndex = i;
	return data;
}

// position in shadow texture space
float SampleDirectionalShadowAtlas (float3 positionSTS) {
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float GetDirectionalShadowAttenuation (DirectionalShadowData data, Surface surfaceWS) {
	float3 positionSTS = mul(
		_DirectionalShadowMatrices[data.tileIndex],
		float4(surfaceWS.position, 1.0)
	).xyz;
	// positionSTS的z分量是深度，采样四个片元，如果z大于阴影贴图中深度，说明被遮挡，记为0，否则记为1，求平均
	float shadow = SampleDirectionalShadowAtlas(positionSTS);
	// 现代GPU处理分支没那么慢，而且对于同一个光源，都会走同一个分支（GPU并行时只要分支被一个片元跑到，其它片元都会跑）
	if (data.strength <= 0.0) {
		return 1.0;
	}
	return lerp(1.0, shadow, data.strength);
}

#endif