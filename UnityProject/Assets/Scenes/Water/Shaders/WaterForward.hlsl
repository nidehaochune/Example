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
    o.positionVS = position_inputs.positionVS;

    
      o.tangentWS =   normal_inputs.tangentWS.xyz;
      o.bitangentWS =   normal_inputs.bitangentWS.xyz;
      o.normalWS =   normal_inputs.normalWS.xyz;
    o.positionNDC = position_inputs.positionNDC;
    

    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    return o;
}

half4 frag (v2f i) : SV_Target
{
    float3 positionWS = i.positionWS;
    float3 viewDirWS =  GetWorldSpaceNormalizeViewDir(positionWS);
    float2 screenPos = (i.positionNDC.xy/i.positionNDC.w);
    // float2 screenUV = (i.positionCS.xy/_ScaledScreenParams.xy);
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
    float3 reflectionVector = reflect(-viewDirWS,normalWS);

    float3 reflection = Reflection(reflectionVector,positionWS,0,1,screenPos);
    float3 probeReflection =DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectionVector, 0), unity_SpecCube0_HDR);

    float3 rmDirection = reflect(normalize(i.positionVS.xyz),TransformWorldToViewNormal(normalWS)); ;
    float3 origin = i.positionVS;

    float2 sampleUV;
    float valid;
    float outOfBounds;
    half debug;

    Raymarch(origin,rmDirection,20,0.2,1,sampleUV,valid,outOfBounds,debug);
    float3 sceneColor = SampleSceneColor(sampleUV);

    float3 finalReflection = lerp(reflection,lerp(probeReflection,sceneColor,valid),smoothstep(0,0.3,outOfBounds)); ;
    
    //fresnel
    float fresnel = Remap( Fresnel(normalWS,viewDirWS,5),float2(0,1),float2(0.01,1));

    //depth
    float rawdepth = SampleSceneDepth(screenPos);
    float eyeDepth = LinearEyeDepth(rawdepth,_ZBufferParams);

    //refraction
    float refractionDepth = saturate(Remap(eyeDepth,float2(_RefractionFade.xy),float2(0,1)));
    float2 refractionUV = finalNormalTS.xy*_RefractionIntensity * (1- refractionDepth) + screenPos.xy;

    float3 refraction = SampleSceneColor(refractionUV);

    float waterDepth = LinearEyeDepth( SampleSceneDepth(refractionUV),_ZBufferParams);
    float depth =saturate(Remap( max(0, waterDepth -i.positionNDC.w),_WaterDepthRange.xy,float2(0,1))) ;

    //Color
    float3 color = lerp(_ColorBright,_ColorDeep,depth);
    float3 waterColor = lerp( color * refraction,color,_ColorAlpha);

    //Depth Difference
    float diffDepth = eyeDepth +i.positionVS.b;
    //EdgeFade
    float alpha = saturate(Remap(diffDepth,float2(0,_EdgeFade),float2(0,1)));
    float smoothness = alpha;


    //Water Depth
    waterDepth = max(0,waterDepth - i.positionNDC.w) ;
    float3 edgeColor = saturate(lerp(float4(1,1,1,0),_EdgeColor,pow(waterDepth,0.5))) * refraction;

    float3 finalColor =lerp( saturate(edgeColor + waterColor),finalReflection,fresnel);

   //
    // SurfaceData surface;
    // surface.albedo = float3(0,0,0);
    // surface.specular = float3(0,0,0);
    // surface.metallic = _Metallic;
    // surface.normalTS = finalNormalTS;
    // surface.emission = finalColor;
    // surface.occlusion = 0;
    // surface.alpha = alpha;
    // surface.smoothness = smoothness;
    // surface.clearCoatMask = 0;
    // surface.clearCoatSmoothness = 0;
    //
    // InputData inputData;
    // inputData.positionWS = i.positionVS;
    // inputData.normalWS = normalWS;
    // inputData.positionCS = i.positionCS;
    // inputData.viewDirectionWS = viewDirWS;
    // inputData.shadowCoord = TransformWorldToShadowCoord(i.positionVS);
    // inputData.fogCoord = 0;
    // inputData.vertexLighting = float3(0,0,0);
    // inputData.bakedGI = float3(0,0,0);
    // inputData.normalizedScreenSpaceUV = normalize(i.positionNDC.xy) ;
    // inputData.shadowMask = 0;
    // inputData.tangentToWorld = tangentToWorld;
    //
    // float3 Color = UniversalFragmentPBR(inputData,surface);
    
    
    return float4(finalColor,alpha);
}


#endif