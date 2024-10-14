#ifndef WATER_INCLUDE
#define WATER_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

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
    float3 positionVS :TEXCOORD5;
    float4 positionNDC :TEXCOORD6;

};

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

float Remap(float In, float2 InMinMax, float2 OutMinMax)
{
    return OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}

float Fresnel(float3 normalWS , float3 viewDirWS,float power)
{
    return pow((1.0 - saturate(dot(normalize(normalWS), normalize(viewDirWS)))), power);
}

float3 Reflection(float3 reflection, float3 positionWS, float perceptualRoughness,float occulusion,float2 normalizedSSUV)
{
    return GlossyEnvironmentReflection(reflection,positionWS,perceptualRoughness,occulusion,normalizedSSUV) ;
}

float3 ViewPosFromDepth(float2 positionNDC, float deviceDepth)
{
    float4 positionCS  = ComputeClipSpacePosition(positionNDC, deviceDepth);
    float4 hpositionVS = mul(UNITY_MATRIX_I_P, positionCS);
    return hpositionVS.xyz / hpositionVS.w;
}

float2 ViewSpacePosToUV(float3 pos)
{
    return ComputeNormalizedDeviceCoordinates(pos, UNITY_MATRIX_P);
}

half OutOfBoundsFade(half2 uv)
{
    half2 fade = 0;
    fade.x = saturate(1 - abs(uv.x - 0.5) * 2);
    fade.y = saturate(1 - abs(uv.y - 0.5) * 2);
    return fade.x * fade.y;
}
void Raymarch(float3 origin, float3 direction, half steps, half stepSize, half thickness, out half2 sampleUV, out half valid, out half outOfBounds, out half debug)
{
    sampleUV = 0;
    valid = 0;
    outOfBounds = 0;
    debug = 0;

    float3 baseOrigin = origin;
    
    direction *= stepSize;
    const half rcpStepCount = rcp(steps);
    
    [loop]
    for(int i = 0; i < steps; i++)
    {
        debug++;
        //if(valid == 0)
        {
            origin += direction;
            direction *= 1.5;
            sampleUV = ViewSpacePosToUV(origin);

            outOfBounds = OutOfBoundsFade(sampleUV);
            
            //return;
            
            if(!(sampleUV.x > 1 || sampleUV.x < 0 || sampleUV.y > 1 || sampleUV.y < 0))
            {
                float deviceDepth = SampleSceneDepth(sampleUV);
                float3 samplePos = ViewPosFromDepth(sampleUV, deviceDepth);

                if(distance(samplePos.z, origin.z) > length(direction) * thickness) continue;

                if(samplePos.z > origin.z)
                {
                    valid = 1;
                    return;
                }
                
            } else
            {
                //outOfBounds = OutOfBoundsFade(sampleUV);
                return;
            }
        }
    }
}

#endif