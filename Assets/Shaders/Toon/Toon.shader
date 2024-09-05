
Shader "Effirot/Toon + Outline" {
    Properties{
        [Header(Surface)]

        [MainTexture] 
        _Texture("Texture", 2D) = "white" {}
        [MainColor] 
        _Color("Color", Color) = (1, 1, 1, 1)

        
        [Header(Outline)]
		
        [HDR]
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width", Range(1.0, 20)) = 15
    }
    
    SubShader {
        
        Tags {
            "Queue"="Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }        
                
        Pass {
            
            Name "ToonLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            } 

            HLSLPROGRAM 

            #define _SPECULAR_COLOR

#if UNITY_VERSION >= 202120
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#else
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
#endif
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex vert
            #pragma fragment frag

            #include "ToonLitPass.hlsl"
            ENDHLSL
        }

        Pass 
		{
            Name "Outline"
            Cull Off
            ZWrite Off
            // ZTest [_ZTest]
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
      
            Stencil {
                Ref 1
                Comp NotEqual
            }

            Tags 
            {
                "RenderType" = "Transparent"
                "IgnoreProjector"="True"
                // "Queue"="Transparent"
            }
            
			HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

			#include "Outline.hlsl"
            
			ENDHLSL
		}

        Pass {
            
            Name "ToonShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "ToonShadowCaster.hlsl"
            ENDHLSL
        }
    }
}