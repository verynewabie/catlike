#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface {
	float3 position;
	float3 normal;
	float3 interpolatedNormal;
	float3 viewDirection;
	float depth;
	float3 color;
	float alpha;
	float metallic;
	// 遮罩为0代表无光
	float occlusion;
	float smoothness;
	float fresnelStrength;
	float dither;
};

#endif