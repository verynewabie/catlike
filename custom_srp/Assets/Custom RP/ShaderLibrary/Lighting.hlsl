#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "BRDF.hlsl"

float3 IncomingLight (Surface surface, Light light)
{
	// saturate将值限制在0~1吗，相当于Clamp(0,1)，但用的比较多，就搞了一个saturate(饱和)
	return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 GetLighting (Surface surface, BRDF brdf, Light light)
{
	return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting (Surface surfaceWS, BRDF brdf, GI gi)
{
	ShadowData shadowData = GetShadowData(surfaceWS);
	float3 color = gi.diffuse;
	for (int i = 0; i < GetDirectionalLightCount(); i++)
	{
		Light light = GetDirectionalLight(i, surfaceWS, shadowData);
		color += GetLighting(surfaceWS, brdf, light);
	}
	return color;
}

#endif