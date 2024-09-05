
Shader "Effirot\Outline"
{
    Properties
    {		
		[HDR]
        _OutlineColor("Outline Color", Color)=(1,1,1,1)
        _OutlineWidth("Outline Width", Range(1.0,15))=1.1
    }
	
    SubShader
    {
        Pass 
		{
			LOD 500

            Name "Outline"
			Cull Front
            Tags 
            {
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