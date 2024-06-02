Shader "Unlit/AnoHair"
{
    Properties
    {
        _Color("Color",Color) = (1,1,1,1)
        _MainTex ("_MainTex", 2D) = "white" {}
    	_NormalTex("_NormalTex",2D) = "white"{}
        _AnioTex("_AnioTex",2D) = "white"{}
        _NormalScale("_NormalScale",Range(0, 5)) = 1
    	
    	_Specular ("Specular Amount", Range(0, 5)) = 1.0 
        _SpecularColor ("Specular Color1", Color) = (1,1,1,1)
        _SpecularColor2 ("Specular Color2", Color) = (0.5,0.5,0.5,1)
		_SpecularMultiplier ("Specular Power1", float) = 100.0
		_SpecularMultiplier2 ("Secondary Specular Power", float) = 100.0
		
		_PrimaryShift ( "Specular Primary Shift", float) = 0.0
		_SecondaryShift ( "Specular Secondary Shift", float) = .7
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct appdata
            {
                float4 positionOS : POSITION;
                float4 normalOS : NORMAL;
                float4 tangentOS :TANGENT;
                float4 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
				float4 TtoW0 : TEXCOORD1;  
				float4 TtoW1 : TEXCOORD2;  
				float4 TtoW2 : TEXCOORD3;
            };

            //获取头发高光
			half StrandSpecular ( half3 T, half3 V, half3 L, half exponent)
			{
				half3 H = normalize(L + V);
				half dotTH = dot(T, H);
				half sinTH = sqrt(1 - dotTH * dotTH);
				half dirAtten = smoothstep(-1, 0, dotTH);
				return dirAtten * pow(sinTH, exponent);
			}
			
			//沿着法线方向调整Tangent方向
			half3 ShiftTangent ( half3 T, half3 N, half shift)
			{
				return normalize(T + shift * N);
			}

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalTex); SAMPLER(sampler_NormalTex);
            TEXTURE2D(_AnioTex); SAMPLER(sampler_AnioTex);

            float4 _MainTex_ST;
            float4 _NormalTex_ST;
            float4 _Color;
            float _NormalScale;

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(v.normalOS,v.tangentOS);
                o.positionCS = vertexInput.positionCS;
                float3 positionWS = vertexInput.positionWS;
                float3 normalWS = normalInput.normalWS;
                float3 worldTangent = normalInput.tangentWS;
                float3 bitNormal = cross(normalWS,worldTangent)*v.tangentOS.w;
                o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.zw = TRANSFORM_TEX(v.uv, _NormalTex);
                o.TtoW0 = float4(worldTangent.x, bitNormal.x, normalWS.x, positionWS.x);
				o.TtoW1 = float4(worldTangent.y, bitNormal.y, normalWS.y, positionWS.y);
				o.TtoW2 = float4(worldTangent.z, bitNormal.z, normalWS.z, positionWS.z);  

                return o;
            }
			float _Specular;
            float _PrimaryShift;
            float _SecondaryShift;
            float _SpecularMultiplier;
            float _SpecularMultiplier2;

            float4 _SpecularColor;
            float4 _SpecularColor2;

            half4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half4 col = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv) * _Color;
                				//法线相关
				half3 bump = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalTex,sampler_NormalTex, i.uv.zw),_NormalScale);
				half3 worldNormal = normalize(half3(dot(i.TtoW0.xyz, bump), dot(i.TtoW1.xyz, bump), dot(i.TtoW2.xyz, bump)));
				float3 worldPos = float3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);
				half3 worldTangent = normalize(half3(i.TtoW0.x, i.TtoW1.x, i.TtoW2.x));
				half3 worldBinormal = normalize(half3(i.TtoW0.y, i.TtoW1.y, i.TtoW2.y));

				half3 worldViewDir = normalize(GetWorldSpaceViewDir(worldPos));
            	Light mainLight= GetMainLight();
				half3 worldLightDir = normalize(mainLight.direction);

                half3 spec = SAMPLE_TEXTURE2D(_AnioTex,sampler_AnioTex,i.uv.xy);
            	half specTex = spec.g;
				half3 t1 = ShiftTangent(worldBinormal, worldNormal, _PrimaryShift + specTex);
				half3 t2 = ShiftTangent(worldBinormal, worldNormal, _SecondaryShift + specTex);
				//计算高光强度
				half3 spec1 = StrandSpecular(t1, worldViewDir, worldLightDir, _SpecularMultiplier)* _SpecularColor;
				half3 spec2 = StrandSpecular(t2, worldViewDir, worldLightDir, _SpecularMultiplier2)* _SpecularColor2;
	
				half4 finalColor = 0;
				finalColor.rgb = col + spec1 * _Specular;//第一层高光
				finalColor.rgb += spec2 * _SpecularColor2 * spec.b * _Specular;//第二层高光，spec.b用于添加噪点
            					finalColor.rgb *= mainLight.color.xyz;//受灯光影响
				finalColor.a = col.a;
                return finalColor;
            }
            ENDHLSL
        }
    }
}
