#ifndef WATER_INCLUDE
#define WATER_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct appdata
{
    float4 positionOS : POSITION;
    float4 normalOS :NORMAL;
    float4 tangent :TANGENT;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float3 positionWS :TEXCOORD1;
    float4 positionCS : SV_POSITION;
    float3 tangentWS :TEXCOORD2;
    float3 bitangentWS :TEXCOORD3;
    float3 normalWS :TEXCOORD4;

};

TEXTURE2D( _MainTex ); SAMPLER(sampler_MainTex);
TEXTURE2D(_Normal_Map);SAMPLER(sampler_Normal_Map);
TEXTURE2D( _Blend_Map ); SAMPLER(sampler_Blend_Map);

float _Metallic;
float _NormalTiling1 ;
float _NormalTiling2;
float _NormalMapStrength;
float _OffsetSpeed;
float _BlendTiling;
float _RefractionIntensity;
float _EdgeFade;
float _ColorAlpha;

float4 _BlendOffsetSpeed;
float4 _RefractionFade;
float4 _EdgeColor;
float4 _WaterDepthRange;
float4 _ColorBright;
float4 _ColorDeep;

float4 _MainTex_ST;

float3 Reflection(float3 reflection, float3 positionWS, float perceptualRoughness,float occulusion,float2 normalizedSSUV)
{
    float3 finalReflection;

    return GlossyEnvironmentReflection(reflection,positionWS,perceptualRoughness,occulusion,normalizedSSUV) ;
}


#endif