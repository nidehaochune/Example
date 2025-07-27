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


            [domain("tri")]
            [outputcontrolpoints(3)]
            [outputtopology("triangle_cw")]
            [partitioning("integer")]
            [patchconstantfunc("patchConstantFunction")]
            vertexInput hull(InputPatch<vertexInput, 3> patch, uint id : SV_OutputControlPointID)
            {
                return patch[id];
            }

            [domain("tri")]
            vertexOutput domain(TessellationFactors factors, OutputPatch<vertexInput, 3> patch,
                               float3 barycentricCoordinates : SV_DomainLocation)
            {
                vertexInput v;

                #define MY_DOMAIN_PROGRAM_INTERPOLATE(fieldName) v.fieldName = \
                    patch[0].fieldName * barycentricCoordinates.x + \
                    patch[1].fieldName * barycentricCoordinates.y + \
                    patch[2].fieldName * barycentricCoordinates.z;

                    MY_DOMAIN_PROGRAM_INTERPOLATE(vertex)
                    MY_DOMAIN_PROGRAM_INTERPOLATE(normal)
                    MY_DOMAIN_PROGRAM_INTERPOLATE(tangent)

                return tessVert(v);
            }

            [maxvertexcount(BLADE_SEGMENTS*2 +1)]
            void geo(triangle VertexOutputGeo input[3]:SV_POSITION, inout TriangleStream<GeoData> triStream)
            {
                float3 pos = input[0].vertex.xyz;
                float3 normal = input[0].normal;
                float4 tangent = input[0].tangent;
                float3 bitangent = cross(normal, tangent.xyz) * tangent.w;
                float3x3 tangentToLocal = float3x3
                (
                    tangent.x, bitangent.x, normal.x,
                    tangent.y, bitangent.y, normal.y,
                    tangent.z, bitangent.z, normal.z
                );
                float height = (rand(pos.zyx) * 2 - 1) * _BladeHeightRandom + _BladeHeight;
                float width = (rand(pos.xzy) * 2 - 1) * _BladeWidthRandom + _BladeWidth;

                //wind uv
                float2 uv = pos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
                float2 windSample = (SAMPLE_TEXTURE2D_X_LOD(_WindDistortionMap, sampler_WindDistortionMap, uv, 0).xy * 2
                    - 1) * _WindStrength;
                //wind
                float3 wind = normalize(float3(windSample.x, windSample.y, 0)); //Wind Vector
                float3x3 windRotation = AngleAxis3x3(PI * windSample, wind);


                float3x3 facingRotationMatrix = AngleAxis3x3(rand(pos) * TWO_PI, float3(0, 0, 1));
                float3x3 bendRotationMatrix = AngleAxis3x3(rand(pos.zzx) * _BendRotationRandom * PI * 0.5,
                                                                        float3(-1, 0, 0));
                float3x3 transformationMatrix = mul(mul(mul(tangentToLocal, facingRotationMatrix), bendRotationMatrix),
                    windRotation);
                float3x3 transformationMatrixFacing = mul(tangentToLocal, facingRotationMatrix);


                float forward = rand(pos.yyz) * _BladeForward;
                for (int i = 0; i < BLADE_SEGMENTS; i++)
                {
                    float t = i / (float)BLADE_SEGMENTS;
                    float segmentHeight = height * t;
                    float segmentWidth = width * (1 - t);
                    float segmentForward= pow(t,_BladeCurve)*forward;

                    float3x3 transformMateix = i == 0 ? transformationMatrixFacing : transformationMatrix;

                    triStream.Append(
                        GenerateGrassVertex(pos, segmentWidth, segmentHeight,segmentForward, float2(0, t), transformMateix));
                    triStream.Append(
                        GenerateGrassVertex(pos, -segmentWidth, segmentHeight,segmentForward, float2(1, t), transformMateix));
                }

                triStream.Append(GenerateGrassVertex(pos, 0, height,forward, float2(0.5, 1), transformationMatrix));
            }

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