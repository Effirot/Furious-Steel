
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#pragma target 3.0

TEXTURE2D(_Texture); SAMPLER(sampler_Texture);

float4 _Texture_ST; 
float4 _Color;

float3 _WorldSpaceLightPos0;

struct appdata {
	float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float2 uv : TEXCOORD0;
};

struct v2f {
	float4 positionCS : SV_POSITION;

	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
};

v2f vert(appdata input) {
	v2f output;

	VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
	VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

	output.positionCS = posnInputs.positionCS;
	output.uv = TRANSFORM_TEX(input.uv, _Texture);
	output.normalWS = normInputs.normalWS;
	output.positionWS = posnInputs.positionWS;

	return output;
}

float4 frag(v2f input) : SV_TARGET{
	float2 uv = input.uv;
	float4 colorSample = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, uv);

	InputData lightingInput = (InputData)0;
	lightingInput.positionWS = input.positionWS;
	lightingInput.normalWS = normalize(input.normalWS);
	lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
	lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS); 

	SurfaceData surfaceInput = (SurfaceData)0;
	surfaceInput.albedo = 1;
	surfaceInput.alpha = colorSample.a * _Color.a;
	surfaceInput.smoothness = 0;

	float4 fragmentData = UniversalFragmentPBR(lightingInput, surfaceInput);

	
	float depthLight = max(max(fragmentData.r, fragmentData.g), max(fragmentData.b, fragmentData.a));
	
	float scribbleDirtectionValue = 
	_WorldSpaceLightPos0.x * input.positionWS.x + 
	_WorldSpaceLightPos0.y * input.positionWS.y + 
	_WorldSpaceLightPos0.z * input.positionWS.z;
	float scribbleDirtectionValueAlt = 
	_WorldSpaceLightPos0.x * input.positionWS.y + 
	_WorldSpaceLightPos0.y * input.positionWS.x + 
	_WorldSpaceLightPos0.z * input.positionWS.z;
	
	float scribble = sin(scribbleDirtectionValueAlt * 300); 
	scribble = min(scribble, sin(scribbleDirtectionValue * 300)); 
	scribble = clamp(scribble - (1.5 - depthLight * 3) + dot(input.normalWS, _WorldSpaceLightPos0), 0, 1);
	// scribble = 1;
	
	float normalizedLight = lerp(0.2, 1, floor(1 + depthLight / 1.5) * 1.5);
	normalizedLight = clamp(normalizedLight, 0, 3);
	
	if (normalizedLight == 3)
	{
		normalizedLight *= 3;
	}

	fragmentData += 1;
	fragmentData *= 3;
	fragmentData = normalize(fragmentData); 

	return scribble * _Color * colorSample * normalizedLight * fragmentData;
}