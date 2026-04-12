#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#include "Surface.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

// 更清晰，尽管这对于我们支持的平台来说并无区别
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
// 阴影贴图采样方式，因为常规的双线性过滤对于深度数据而言并不适用
// 采样器状态可以通过在其名称中包含特定关键词来内联定义：linear 使用线性过滤，clamp 使用clamp寻址模式，compare 启用深度比较功能
#define SHADOW_SAMPLER sampler_point_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
	int _CascadeCount;
	float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
	float4 _CascadeData[MAX_CASCADE_COUNT];
	float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
	float4 _ShadowAtlasSize;
	// 1/maxDistance 1/distanceFade 1/(1-cascadeFade*cascadeFade)
	float4 _ShadowDistanceFade;
CBUFFER_END

struct DirectionalShadowData {
	float strength;
	int tileIndex;
	float normalBias;
	int shadowMaskChannel;
};

struct ShadowMask {
	bool always;
	bool distance;
	// 四个分量，分别表示四盏灯，为1代表照亮无阴影，默认值为1，最重要的光是R，重要程度可参考强度
	// Unity会将前四个灯光之外的所有混合模式灯光转换为完全烘焙的灯光。这是基于所有灯光都是平行光的假设，而平行光是我们目前唯一支持的光类型
	// 其他光类型的影响范围有限，因此有可能为多个灯光使用相同的通道。
	float4 shadows;
};

struct ShadowData {
	int cascadeIndex;
	// 最后一级该值为1，越接近下一级该值越接近0
	float cascadeBlend;
	float strength;
	ShadowMask shadowMask;
};

float FadedShadowStrength (float distance, float scale, float fade) {
	return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData (Surface surfaceWS) {
	ShadowData data;
	data.shadowMask.always = false;
	data.shadowMask.distance = false;
	data.shadowMask.shadows = 1.0;
	data.cascadeBlend = 1.0;
	data.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
	int i;
	for (i = 0; i < _CascadeCount; i++) {
		float4 sphere = _CascadeCullingSpheres[i];
		float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
		if (distanceSqr < sphere.w) {
			// 这里是一个平滑且接近线性的Fade（与d/r接近线性）
			float fade = FadedShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
			// 最后一级分辨率低且会突然消失，直接乘到strength上
			if (i == _CascadeCount - 1)
			{
				data.strength *= fade;
			}
			else
			{
				data.cascadeBlend = fade;
			}
			break;
		}
	}

	if (i == _CascadeCount) {
		data.strength = 0.0;
	}
#if defined(_CASCADE_BLEND_DITHER)
	else if (data.cascadeBlend < surfaceWS.dither) {
		i += 1;
	}
#endif
#if !defined(_CASCADE_BLEND_SOFT)
	data.cascadeBlend = 1.0;
#endif
	data.cascadeIndex = i;
	return data;
}

// position in shadow texture space
float SampleDirectionalShadowAtlas (float3 positionSTS) {
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow (float3 positionSTS) {
	#if defined(DIRECTIONAL_FILTER_SETUP)
	float weights[DIRECTIONAL_FILTER_SAMPLES];
	float2 positions[DIRECTIONAL_FILTER_SAMPLES];
	float4 size = _ShadowAtlasSize.yyxx;
	// size:1/atlasSize,1/atlasSize,atlasSize,atlasSize 采样位置 权重 位置的输出参数
	DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
	float shadow = 0;
	for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) {
		shadow += weights[i] * SampleDirectionalShadowAtlas(
			float3(positions[i].xy, positionSTS.z)
		);
	}
	return shadow;
	#else
	return SampleDirectionalShadowAtlas(positionSTS);
	#endif
}

float GetCascadedShadow (DirectionalShadowData directional, ShadowData global, Surface surfaceWS) {
	float3 normalBias = surfaceWS.normal *(directional.normalBias * _CascadeData[global.cascadeIndex].y);
	float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex],float4(surfaceWS.position + normalBias, 1.0)).xyz;
	float shadow = FilterDirectionalShadow(positionSTS);
	if (global.cascadeBlend < 1.0) {
		normalBias = surfaceWS.normal *(directional.normalBias * _CascadeData[global.cascadeIndex + 1].y);
		positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1],float4(surfaceWS.position + normalBias, 1.0)).xyz;
		shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
	}
	return shadow;
}

float GetBakedShadow (ShadowMask mask, int channel) {
	float shadow = 1.0;
	if (mask.always || mask.distance) {
		if (channel >= 0) {
			/*
			 * GPU 硬件对于 点积 (dp4) 指令的执行效率极高，且是单指令完成的矢量操作
			 * 而直接根据变量索引寄存器通道（mov dst, src[aL]）在某些旧架构或特定并行线程组中可能会产生额外的开销或寄存器依赖
			 * 编译器为了通用性和吞吐量，倾向于统一转换为点积。
			 * 理论上我们也可以手写点积并把那个筛选向量从 CPU 传到 GPU。但这样做毫无意义，因为：你还是得根据 channel 去索引那个筛选向量数组。多传了四个 float4 常量数据。
			 * 结论就是：别折腾，写 mask[i] 就好，剩下的交给编译器。
float4 channelVectors[4] = {
	float4(1,0,0,0),
	float4(0,1,0,0),
	float4(0,0,1,0),
	float4(0,0,0,1)
};
float shadowMaskValue = dot(mask.Shadows, channelVectors[channel]);
操作					索引对象						底层后果
直接索引通道			矢量寄存器的子字段 (R/G/B/A)	产生动态通道选择指令，需要线程组同步等待。
索引常量数组 + 点积	常量显存中的完整矢量			通道索引变成了立即数地址偏移，配合单周期 dp4 指令。
可能是GPU更擅长去拿向量而不是找其中某个分量？
			 */
			shadow = mask.shadows[channel];
		}
	}
	return shadow;
}

float GetBakedShadow (ShadowMask mask, int channel, float strength) {
	if (mask.always || mask.distance) {
		return lerp(1.0, GetBakedShadow(mask, channel), strength);
	}
	return 1.0;
}

float MixBakedAndRealtimeShadows (ShadowData global, float shadow, int shadowMaskChannel, float strength) {
	float baked = GetBakedShadow(global.shadowMask, shadowMaskChannel);
	// Always 模式：Shadow Map 剔除了静态投影物体，只包含动态物体的阴影。而烘焙阴影只记录静态物体的阴影。二者是不同物体产生的阴影，需要同时生效。
	if (global.shadowMask.always) {
		shadow = lerp(1.0, shadow, global.strength);
		shadow = min(baked, shadow);
		return lerp(1.0, shadow, strength);
	}
	if (global.shadowMask.distance) {
		shadow = lerp(baked, shadow, global.strength);
		return lerp(1.0, shadow, strength);
	}
	return lerp(1.0, shadow, strength * global.strength);
}

float GetDirectionalShadowAttenuation (DirectionalShadowData directional, ShadowData global, Surface surfaceWS) {
	#if !defined(_RECEIVE_SHADOWS)
	return 1.0;
	#endif
	float shadow;
	if (directional.strength * global.strength <= 0.0) {
		shadow = GetBakedShadow(global.shadowMask,  directional.shadowMaskChannel, abs(directional.strength));
	}
	else {
		shadow = GetCascadedShadow(directional, global, surfaceWS);
		shadow = MixBakedAndRealtimeShadows(global, shadow, directional.shadowMaskChannel, directional.strength);
	}
	return shadow;
}

#endif