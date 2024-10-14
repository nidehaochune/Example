Shader "Unlit/WaterLine"
{
    Properties
    {
        _SpeedX("SpeedX", Float) = 5
        _SpeedZ("SpeedZ", Float) = 3
        _FX("FX", Float) = 0.05
        _FZ("FZ", Float) = 0.05
        _PZ("PZ", Float) = 0.89
        _PX("PX", Float) = 2
        _DepthSmoothStep(" Depth SmoothStep ",Vector) = (0.55,1.08,0,0)
        _DepthColor("Depth Color",Color) = (1,1,1,1)
        _DepthWSYST("Depth WSYST",Vector) = (0,0,0,0)
        _WaterLine("WaterLine", Color) = (1,0,0,1)
        _BackLightPower("Back Light Power",Vector) = (1,10,0,0)
        _ScreenUVOffset("ScreenUVOffset", Vector) = (0,0,0,0)
        _Offset("Offset", Float) = 1.53
        _WaterDown("WaterDown", Color) = (1,1,1,1)
        _OffsetVector("Offset Vector",Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"  "LightMode"="UniversalForward"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "WaterLineInclude.hlsl"


            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                
                half wave = WaterWave(i.positionWS);
                half waterLine = (i.positionWS.y- wave);
                half waterLine01 = saturate(waterLine);
                half Offset =saturate(1 -distance(saturate(smoothstep(_OffsetVector.x,_OffsetVector.y,waterLine+_Offset*0.01)),0.5));
                // Offset = saturate(Offset)
                half smoothStepOffset = smoothstep(0.5,1,Offset);

                half alpha = _WaterDown.a * smoothstep(0.8,1,saturate((step(waterLine,0)+Offset))) ;


                half4 waterDownColor = lerp(float4(0,0,0,0),_WaterDown,step(waterLine,0));
                half4 waterDownAndLineColor = waterDownColor * (1- smoothStepOffset) +  _WaterLine * smoothStepOffset *waterDownColor ;

                float4 screenUV=  i.screenPos/i.screenPos.w;

                float rawDepth = SampleSceneDepth(screenUV.xy);
                // float depth =smoothstep(_DepthSmoothStep.x,_DepthSmoothStep.y,rawDepth) ;

                float4 sceneColor = float4(SampleSceneColor(screenUV.xy),1);

                half finalDepth = saturate(smoothstep(_DepthSmoothStep.x,_DepthSmoothStep.y,1-rawDepth));
                float4 Color = lerp(sceneColor,_DepthColor,finalDepth);
                Light light = GetMainLight();
                float3 worldViewDir = ( _WorldSpaceCameraPos.xyz - i.positionWS );
				worldViewDir = normalize(worldViewDir);
                half3 finalColor = smoothstep(_BackLightPower.x,_BackLightPower.y,dot(-light.direction,worldViewDir)) * _MainLightColor * finalDepth ;

                finalColor = finalColor + Color *  waterDownAndLineColor;
                
                return half4(finalColor,alpha);
            }
            ENDHLSL
        }
    }
}