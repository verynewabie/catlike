Shader "Custom RP/Lit"
{
	Properties
	{
		// {}是很久以前的用法，现在用它只是为了防止奇怪的错误
		_BaseMap("Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)]
		_Clipping ("Alpha Clipping", Float) = 0
		// 金属度
		_Metallic ("Metallic", Range(0, 1)) = 0
		// 光滑度
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5
		[Enum(UnityEngine.Rendering.BlendMode)]
		_SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)]
		_DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)]
		_ZWrite ("Z Write", Float) = 1
		[Toggle(_PREMULTIPLY_ALPHA)]
		_PremulAlpha ("Premultiply Alpha", Float) = 0
		[KeywordEnum(On, Clip, Dither, Off)]
		_Shadows ("Shadows", Float) = 0
		[Toggle(_RECEIVE_SHADOWS)]
		_ReceiveShadows ("Receive Shadows", Float) = 1
	}
	CustomEditor "CustomShaderGUI"
	SubShader
	{
		Pass
		{
			Tags{ "LightMode" = "CustomLit" }
			// _SrcBlend的运行时修改有效，但会导致重新编译Shader
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			
			HLSLPROGRAM

			// 有些低版本还不支持变量循环，这里我们设置一个支持的版本
			#pragma target 3.5
			
			#pragma shader_feature _PREMULTIPLY_ALPHA
			#pragma shader_feature _CLIPPING
			// _ means default:2x2
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
			#pragma shader_feature _RECEIVE_SHADOWS
			#pragma multi_compile_instancing
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment
			#include "LitPass.hlsl"
			
			ENDHLSL
		}

		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}

			// 只用写入深度，不用写入颜色
			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			// _ for on and off
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma multi_compile_instancing
			#include "ShadowCasterPass.hlsl"
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			
			ENDHLSL
		}
	}
}