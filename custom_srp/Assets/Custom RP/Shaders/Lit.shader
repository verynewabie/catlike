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
		[Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0
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
			#pragma shader_feature _CLIPPING
			#pragma shader_feature _PREMULTIPLY_ALPHA
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
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#include "ShadowCasterPass.hlsl"
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			
			ENDHLSL
		}
	}
}