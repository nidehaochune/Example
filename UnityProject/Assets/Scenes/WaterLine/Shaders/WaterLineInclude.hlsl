#ifndef WATER_LINE_INCLUDE
#define WATER_LINE_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
struct appdata
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1 ;
};

struct v2f
{
    // float2 uv : TEXCOORD0;
    float4 positionCS : SV_POSITION;
	float4 screenPos : TEXCOORD1;
	float3 positionWS :TEXCOORD2;
};


float _PX;
float _PZ;
float _SpeedX;
float _SpeedZ;
float _Offset;

float4 _OffsetVector;
float4 _WaterDown;
float4 _WaterLine;
float4 _ScreenUVOffset;
float4 _DepthSmoothStep;

float4 _DepthWSYST;
float4 _DepthColor;
float4 _BackLightPower;


float RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
{
	original -= center;
	float C = cos( angle );
	float S = sin( angle );
	float t = 1 - C;
	float m00 = t * u.x * u.x + C;
	float m01 = t * u.x * u.y - S * u.z;
	float m02 = t * u.x * u.z + S * u.y;
	float m10 = t * u.x * u.y + S * u.z;
	float m11 = t * u.y * u.y + C;
	float m12 = t * u.y * u.z - S * u.x;
	float m20 = t * u.x * u.z - S * u.y;
	float m21 = t * u.y * u.z + S * u.x;
	float m22 = t * u.z * u.z + C;
	float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
	return mul( finalMatrix, original ) + center;
}

float WaterWave(float3 postionWS)
{
	float3 positionWS = postionWS;
	float PX = postionWS.x * _PX; 
	float PZ = postionWS.z * _PZ;

	float speedX = _SpeedX * _TimeParameters.x;
	float speedZ = _SpeedZ * _TimeParameters.z;

	float wave_z = sin(PZ + speedZ) * 0.05;
	float wave_y = cos(PX + speedX) * 0.06;

	float3 rotatedValue = RotateAroundAxis( float3( 0,0,0 ), positionWS, float3(0,1,0), 60.0 );
	float wave_x = sin(rotatedValue.x * 5 + _TimeParameters * 10) * 0.005;  
	float wave_w = sin(rotatedValue.z *10 + _TimeParameters * 10) * 0.01;
	
    float wave = wave_x+ wave_z + wave_y + wave_w ;
    
    return wave;
}

#endif