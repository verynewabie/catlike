#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

float3 IncomingLight (Surface surface, Light light) {
	// saturate将值限制在0~1吗，相当于Clamp(0,1)，但用的比较多，就搞了一个saturate(饱和)
	return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLighting (Surface surface, Light light) {
	return IncomingLight(surface, light) * surface.color;
}

float3 GetLighting (Surface surface) {
	return GetLighting(surface, GetDirectionalLight());
}

#endif