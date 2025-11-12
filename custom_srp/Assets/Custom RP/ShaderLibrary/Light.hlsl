#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

// 自定义cbuffer，默认全局
#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_CustomLight)
	// float4 _DirectionalLightColor;
	// float4 _DirectionalLightDirection;
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
};

int GetDirectionalLightCount () {
	return _DirectionalLightCount;
}

// 方向是光线来的方向而不是光线射向的方向
Light GetDirectionalLight (int index) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	return light;
}

#endif