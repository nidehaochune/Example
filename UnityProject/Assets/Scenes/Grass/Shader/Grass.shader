Shader "Unlit/Grass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WindDistortionMap("Wind Tex" ,2D) = "white" {}
        _WindFrequency("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)
        _WindStrength("Wind Strength", Float) = 1

        _BottomColor("Bottom Color",Color) = (1,1,1,1)
        _TopColor("Top Color",Color) = (1,1,1,1)
        _BendRotationRandom("Bend Rotation Random", Range(0, 1)) = 0.2

        _BladeWidth("Blade Width", Float) = 0.05
        _BladeWidthRandom("Blade Width Random", Float) = 0.02
        _BladeHeight("Blade Height", Float) = 0.5
        _BladeHeightRandom("Blade Height Random", Float) = 0.3

        _TessellationUniform("Tessellation Uniform", Range(1, 64)) = 1

        _BladeForward("Blade Forward Amount", Float) = 0.38
        _BladeCurve("Blade Curvature Amount", Range(1, 4)) = 2

    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "LightMode" = "UniversalForward"
        }
        LOD 100

        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo
            #pragma hull hull
            #pragma domain domain

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "GrassInput.hlsl"

            half4 frag(GeoData i) : SV_Target
            {
                // sample the texture
                half4 col = lerp(_TopColor, _BottomColor, i.uv.y);

                return col;
            }

            ENDHLSL
        }
        
    }
}