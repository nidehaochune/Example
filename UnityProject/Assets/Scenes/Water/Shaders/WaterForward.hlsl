#ifndef WATER_FORWARD
#define WATER_FORWARD
#include "WaterInclude.hlsl"

v2f vert (appdata v)
{
    v2f o;
    VertexPositionInputs position_inputs = GetVertexPositionInputs(v.positionOS.xyz);
    VertexNormalInputs normal_inputs = GetVertexNormalInputs(v.normalOS.xyz,v.tangent);
    o.positionCS = position_inputs.positionCS;
    o.positionWS = position_inputs.positionWS;

    
      o.tangentWS =   normal_inputs.tangentWS.xyz;
      o.bitangentWS =   normal_inputs.bitangentWS.xyz;
      o.normalWS =   normal_inputs.normalWS.xyz;
    

    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

half4 frag (v2f i) : SV_Target
{
    float3 positionWS = i.positionWS;
    float3 viewDirWS =  GetWorldSpaceNormalizeViewDir(positionWS);
    //Normal Strength
    float2 timeSpeed1 = float2( _TimeParameters.x * _OffsetSpeed * -0.5,0);
    float2 timeSpeed2 = float2( _TimeParameters.x * _OffsetSpeed ,0);

    float2 blendSpeed = _TimeParameters.y * _BlendOffsetSpeed.xy;

    float2 uv1 = positionWS.xz * _NormalTiling1 + timeSpeed1;
    float2 uv2 = positionWS.xz * _NormalTiling2 + timeSpeed2;

    float3 normal1 =UnpackNormal(SAMPLE_TEXTURE2D(_Normal_Map,sampler_Normal_Map,uv1));
    float3 normal2 = UnpackNormal(SAMPLE_TEXTURE2D(_Normal_Map,sampler_Normal_Map,uv2));

    float blendTilling = positionWS.xz*_BlendTiling;
    float blendUV = blendSpeed + blendTilling + blendTilling;
    float blend = SAMPLE_TEXTURE2D(_Blend_Map,sampler_Blend_Map,blendUV).r;
    
    float3 finalNormalTS = normalize(lerp(normal1,normal2,blend));
    float3 normalStrenth = float3(finalNormalTS.rg * _NormalMapStrength,lerp(1,finalNormalTS.b,_NormalMapStrength));

    float3x3 tangentToWorld = float3x3
    (
        i.tangentWS.xyz,
        i.bitangentWS.xyz,
        i.normalWS.xyz
    );
    float3 normalWS = TransformTangentToWorld(normalStrenth,tangentToWorld);


    //Reflection
    // float3 reflection1 = reflect(viewDirWS,normalWS,0);
    float3 reflection1 = reflect(-viewDirWS,normalWS);

    float2 screenuv;
    float3 finalReflection = Reflection(reflection1,positionWS,0,1,screenuv);
    
    
    
    return float4(finalReflection,_ColorAlpha);
}


#endif