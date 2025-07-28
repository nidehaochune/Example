#ifndef GRASS_INPUT
#define GRASS_INPUT

#define BLADE_SEGMENTS 3

struct vertexInput
{
    float4 vertex : POSITION;
    float3 normal :NORMAL;
    float4 tangent : TANGENT;
};

struct VertexOutputGeo
{
    float4 vertex : SV_POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
};

struct vertexOutput
{
    float4 vertex : SV_POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
};

struct GeoData
{
    float4 pos :SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
};

struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

TEXTURE2D(_MainTex);
float4 _MainTex_ST;
SAMPLER(sampler_MainTex);
TEXTURE2D(_WindDistortionMap);
float4 _WindDistortionMap_ST;
SAMPLER(sampler_WindDistortionMap);
float4 _BottomColor;
float4 _TopColor;
float _BendRotationRandom;
float _BladeHeight;
float _BladeHeightRandom;
float _BladeWidth;
float _BladeWidthRandom;

float _TessellationUniform;
float4 _WindFrequency;
float _WindStrength;

float _BladeForward;
float _BladeCurve;

float rand(float3 co)
{
    return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
}

float3x3 AngleAxis3x3(float angle, float3 axis)
{
    float c, s;
    sincos(angle, s, c);
    float t = 1 - c;
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    return float3x3(
        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
        t * x * z - s * y, t * y * z + s * x, t * z * z + c);
}

// struct geometryOutput
// {
//     float4 pos : SV_POSITION;
//     float2 uv : TEXCOORD0;
// };

GeoData VertexOutput(float3 pos, float2 uv)
{
    GeoData o;
    o.pos = TransformObjectToHClip(pos);
    o.worldPos = TransformObjectToWorld(pos);
    o.uv = uv;
    return o;
}

vertexOutput tessVert(vertexInput v)
{
    vertexOutput o;
    // Note that the vertex is NOT transformed to clip
    // space here; this is done in the grass geometry shader.
    o.vertex = v.vertex;
    o.normal = v.normal;
    o.tangent = v.tangent;
    return o;
}

VertexOutputGeo vert(vertexInput v)
{
    VertexOutputGeo o;
    o.vertex = (v.vertex);
    o.normal = v.normal;
    o.tangent = v.tangent;
    return o;
}

TessellationFactors patchConstantFunction(InputPatch<vertexInput, 3> patch)
{
    TessellationFactors f;
    f.edge[0] = _TessellationUniform;
    f.edge[1] = _TessellationUniform;
    f.edge[2] = _TessellationUniform;
    f.inside = _TessellationUniform;
    return f;
}

GeoData GenerateGrassVertex(float3 vertexPosition, float width, float height, float forward, float2 uv,
                            float3x3 transformMatrix)
{
    float3 tangentPoint = float3(width, forward, height);

    float3 localPosition = vertexPosition + mul(transformMatrix, tangentPoint);
    return VertexOutput(localPosition, uv);
}

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
                float2 windSample = (SAMPLE_TEXTURE2D_LOD(_WindDistortionMap, sampler_WindDistortionMap, uv, 0).xy * 2
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


#endif
