#include "UnityCG.cginc"

struct appdata 
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float3 smoothNormal : TEXCOORD3;
    fixed4 color : COLOR;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f 
{
    float4 pos : SV_POSITION;
};

uniform float _OutlineWidth;
uniform float4 _OutlineColor;

v2f vert (appdata v) 
{
    v2f o;
    
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 objPos = mul (unity_ObjectToWorld, float4(0,0,0,1));
    
    float dist = distance(_WorldSpaceCameraPos, objPos.xyz) / _ScreenParams.g;
    float expand = dist * 0.25 * _OutlineWidth;
    float4 pos = float4(v.vertex.xyz + v.normal * expand, 1);

    o.pos = UnityObjectToClipPos(pos);
    return o;
}

float4 frag(v2f i) : COLOR 
{
    return _OutlineColor;
}