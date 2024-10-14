Shader "Hidden/Water"
{
    Properties
    {
        _Metallic("Metallic", Range(0, 1)) = 0
        [Normal][NoScaleOffset]_Normal_Map("Normal Map", 2D) = "bump" {}
        _NormalTiling1("NormalTiling1", Float) = 0.0025
        _NormalTiling2("NormalTiling2", Float) = 0.01
        _NormalMapStrength("NormalMapStrength", Range(0, 1)) = 0.2
        _OffsetSpeed("OffsetSpeed", Float) = 0.2
        [NoScaleOffset]_Blend_Map("Blend Map", 2D) = "white" {}
        _BlendTiling("BlendTiling", Float) = 0
        _BlendOffsetSpeed("BlendOffsetSpeed", Vector) = (0, 0, 0, 0)
        _RefractionFade("RefractionFade", Vector) = (2.3, 17.78, 0, 0)
        _RefractionIntensity("RefractionIntensity", Range(0, 5)) = 0
        _EdgeFade("EdgeFade", Range(0, 1)) = 0.2
        _EdgeColor("EdgeColor", Color) = (0, 0.7647791, 1, 0)
        _WaterDepthRange("WaterDepthRange", Vector) = (0, 0, 0, 0)
        _ColorBright("ColorBright", Color) = (0.1137255, 0.4789602, 0.6235294, 0)
        _ColorDeep("ColorDeep", Color) = (0.09545213, 0.3113208, 0.256497, 0)
        _ColorAlpha("ColorAlpha", Range(0, 1)) = 0
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  }
        LOD 100

        Pass
        {
            Tags{ "LightMode" = "UniversalForward"}
            
            
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WaterForward.hlsl"

            ENDHLSL
        }
    }
}
