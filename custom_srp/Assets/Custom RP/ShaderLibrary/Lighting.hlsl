#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

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

float3 GetLighting (Surface surfaceWS, BRDF brdf)
{
	float3 color = 0.0;
	for (int i = 0; i < GetDirectionalLightCount(); i++)
	{
		color += GetLighting(surfaceWS, brdf, GetDirectionalLight(i, surfaceWS));
	}
	return color;
}

#endif