
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

TEXTURE2D(_Texture); SAMPLER(sampler_Texture);

float4 _Texture_ST; 
float4 _Color;

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

float2 GetScreenSpaceUV(v2f input)
{
	// float2 uv = input.spos.xy / input.spos.w;
	// float4 cpos = UnityObjectToClipPos(float3(0,0,0));
	// uv -= cpos.xy / cpos.w;
	// uv *= cpos.w / UNITY_MATRIX_P._m11;
	// uv.x *= _ScreenParams.x / _ScreenParams.y;

	// return uv
}

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
	surfaceInput.smoothness = 0.5;

	float4 fragmentData = UniversalFragmentPBR(lightingInput, surfaceInput);

	float normalizedFragmetData = fragmentData;
	normalizedFragmetData += 1.4;
	normalizedFragmetData = floor(normalizedFragmetData * 2) / 2;
	normalizedFragmetData = lerp(0.1, 1, normalizedFragmetData);

	// return _Color * colorSample * normalizedFragmetData * normalize(fragmentData);
	return _Color * colorSample * normalizedFragmetData;
}