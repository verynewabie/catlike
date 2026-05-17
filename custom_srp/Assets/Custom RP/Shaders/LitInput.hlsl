#ifndef CUSTOM_LIT_INPUT_INCLUDED
#define CUSTOM_LIT_INPUT_INCLUDED

TEXTURE2D(_MaskMap);
TEXTURE2D(_BaseMap);
TEXTURE2D(_NormalMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_BaseMap);

/*
DetailMap也不要启用sRGB，我们把反射率存储在R通道，平滑度存储在B通道。
一般会将法线向量的XY分量存储在AG通道，但我们的贴图不包含法线向量，因此我们只使用 RB 通道。所以它是 RGB 纹理，而不是 RGBA 纹理。
 */
TEXTURE2D(_DetailMap);
TEXTURE2D(_DetailNormalMap);
SAMPLER(sampler_DetailMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailAlbedo)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailSmoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailNormalScale)
	UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

// 简化，也可以应用到UnlitInput
#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

float2 TransformBaseUV (float2 baseUV) {
	float4 baseST = INPUT_PROP(_BaseMap_ST);
	return baseUV * baseST.xy + baseST.zw;
}

float2 TransformDetailUV (float2 detailUV) {
	float4 detailST = INPUT_PROP(_DetailMap_ST);
	return detailUV * detailST.xy + detailST.zw;
}

struct InputConfig {
	float2 baseUV;
	float2 detailUV;
	bool useMask;
	bool useDetail;
};

InputConfig GetInputConfig (float2 baseUV, float2 detailUV = 0.0) {
	InputConfig c;
	c.baseUV = baseUV;
	c.detailUV = detailUV;
	c.useMask = false;
	c.useDetail = false;
	return c;
}

float4 GetMask (InputConfig c) {
	if (c.useMask)
	{
		return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, c.baseUV);
	}
	return 1.0;
}

float3 GetNormalTS (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, c.baseUV);
	float scale = INPUT_PROP(_NormalScale);
	float3 normal = DecodeNormal(map, scale);

	if (c.useDetail)
	{
		map = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailMap, c.detailUV);
		scale = INPUT_PROP(_DetailNormalScale) * GetMask(c).b;
		float3 detail = DecodeNormal(map, scale);
		normal = BlendNormalRNM(normal, detail);
	}

	return normal;
}

float4 GetDetail (InputConfig c) {
	if (c.useDetail)
	{
		float4 map = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, c.detailUV);
		// 0~1映射到-1~1
		return map * 2.0 - 1.0;
	}
	return 0.0;
}

float4 GetBase (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
	float4 color = INPUT_PROP(_BaseColor);

	if (c.useDetail) {
		// 只有R通道会影响反照率，使其趋向黑色或白色
		float detail = GetDetail(c).r * INPUT_PROP(_DetailAlbedo);;
		// map += detail;
		// 不先用平方根再平方时提亮效果似乎比暗化效果更强，因为我们是在线性空间中进行调整的，用平方根是近似把颜色从linear变为gamma
		float mask = GetMask(c).b;
		// 我们想让一些地方不受或少受Detail影响，就用B通道Mask
		map.rgb = lerp(sqrt(map.rgb), detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
		map.rgb *= map.rgb;
	}

	return map * color;
}

float3 GetEmission (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, c.baseUV);
	float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissionColor);
	return map.rgb * color.rgb;
}

float GetCutoff (InputConfig c) {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic (InputConfig c) {
	float metallic = INPUT_PROP(_Metallic);
	metallic *= GetMask(c).r;
	return metallic;
}

float GetSmoothness (InputConfig c) {
	float smoothness = INPUT_PROP(_Smoothness);
	smoothness *= GetMask(c).a;

	if (c.useDetail) {
		float detail = GetDetail(c).b * INPUT_PROP(_DetailSmoothness);
		float mask = GetMask(c).b;
		smoothness = lerp(smoothness, detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
	}

	return smoothness;
}

float GetOcclusion (InputConfig c) {
	float strength = INPUT_PROP(_Occlusion);
	float occlusion = GetMask(c).g;
	occlusion = lerp(occlusion, 1.0, strength);
	return occlusion;
}

float GetFresnel (InputConfig c) {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Fresnel);
}

#endif
