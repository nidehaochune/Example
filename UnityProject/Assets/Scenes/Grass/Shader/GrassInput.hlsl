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
#endif
