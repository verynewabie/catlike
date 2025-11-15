#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED
#include "Surface.hlsl"

#define MIN_REFLECTIVITY 0.04

float OneMinusReflectivity (float metallic) {
	float range = 1.0 - MIN_REFLECTIVITY;
	return range - metallic * range;
}

struct BRDF
{
	float3 diffuse;
	float3 specular;
	float roughness;
};

BRDF GetBRDF (Surface surface, bool applyAlphaToDiffuse = false)
{
	BRDF brdf;
	// 金属度越高，漫反射越低，漫反射范围0~0.96
	float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
	brdf.diffuse = surface.color * oneMinusReflectivity;
	if (applyAlphaToDiffuse)
		brdf.diffuse *= surface.alpha;
	// 金属度为0时。高光为最小反射率，金属度为1时，高光为表面颜色
	brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
	// PerceptualSmoothnessToPerceptualRoughness是CoreRP库里CommonMaterial的
	float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

	return brdf;
}

// 公式: \frac{r^{2}}{d^{2}max(0.1,(L\cdot H)^{2})n},d=(N \cdot H)^{2}(r^{2}-1)+1.0001,n=4r+2
float SpecularStrength (Surface surface, BRDF brdf, Light light)
{
	// SafeNormalize防止除0错误
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 DirectBRDF (Surface surface, BRDF brdf, Light light)
{
	return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif