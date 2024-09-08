
Shader "Effirot/Outline"
{
    Properties
    {		
		[HDR]
        _OutlineColor("Outline Color", Color)=(1,1,1,1)
        _OutlineWidth("Outline Width", Range(1.0,30))=1.1
    }
	
    SubShader
    {
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
                "Queue"="Transparent" 
                "IgnoreProjector"="True"
                "Queue"="Transparent"
            }
            
			CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

			#include "Outline.hlsl"

            ENDCG
		}
    }
}