
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

	float4 fragmentData;
#if UNITY_VERSION >= 202120
	fragmentData = UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
#else
	fragmentData = UniversalFragmentBlinnPhong(lightingInput, surfaceInput.albedo, float4(surfaceInput.specular, 1), surfaceInput.smoothness, surfaceInput.emission, surfaceInput.alpha);
#endif

	fragmentData = (fragmentData.x + fragmentData.y + fragmentData.z + fragmentData.w) / 4;
	fragmentData += 0.3f;
	fragmentData = lerp(0.2f, 1, floor(fragmentData / 1.3f) + floor(fragmentData / 1.05f));
	
	return _Color * colorSample * fragmentData;
}