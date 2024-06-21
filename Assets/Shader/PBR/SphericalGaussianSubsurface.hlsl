#ifndef SSS
#define SSS

#define PI 3.1415926535

struct FSphericalGaussian
{
    float3	Axis;		// u, \mu
    float	Sharpness;	// L, \lambda
    float	Amplitude;	// a, 
};

FSphericalGaussian MakeNormalizedSG(float3 LightDir, half Sharpness)
{
    FSphericalGaussian SG;

    // Align axis，multiply light Intensity.
    SG.Axis = LightDir;
    SG.Sharpness = Sharpness; // (1 / ScatterAmt.element)
    SG.Amplitude = SG.Sharpness / ((2 * PI) - (2 * PI) * exp(-2 * SG.Sharpness)); // Normalize
        
    // SG.Amplitude *= LightIntensity;
    return SG;
}

float3 DotCosineLobe( FSphericalGaussian G, float3 N )
{
    const float muDotN = dot( G.Axis, N );

    const float c0 = 0.36;
    const float c1 = 0.25 / c0;

    float eml  = exp( -G.Sharpness );
    float em2l = eml * eml;
    float rl   = rcp( G.Sharpness );
        
    float scale = 1.0f + 2.0f * em2l - rl;
    float bias  = (eml - em2l) * rl - em2l;

    float x = sqrt( 1.0 - scale );
    float x0 = c0 * muDotN;
    float x1 = c1 * x;

    float n = x0 + x1;
    float y = ( abs( x0 ) <= x1 ) ? n * n / x : saturate( muDotN );

    return scale * y + bias;
}

float3 SGSGDiffuseLighting (float3 N, float3 lightDir, float3 ScatterAmt)
{
    FSphericalGaussian redKernel   = MakeNormalizedSG(lightDir, 1.0f / max(ScatterAmt.x, 0.0001f));
    FSphericalGaussian greenKernel = MakeNormalizedSG(lightDir, 1.0f / max(ScatterAmt.y, 0.0001f));
    FSphericalGaussian blueKernel  = MakeNormalizedSG(lightDir, 1.0f / max(ScatterAmt.z, 0.0001f));

    float3 SGDiffuse = float3(DotCosineLobe(redKernel, N).x,
    DotCosineLobe(greenKernel, N).x,
    DotCosineLobe(blueKernel, N).x);

    // Below is Diffuse Tonemapping, without it, sss will be purple or grey.
    // Cuz too much red radiance scatter out from skin.
    // Tonemap_Filmic_UC2DefaultToGamma
    // Uncharted II fixed tonemapping formula.
    // The linear to sRGB conversion is baked in.
    half3 x = max(0, (SGDiffuse - 0.004));
    half3 DiffuseTonemapping = \
    lerp(SGDiffuse, (x * (6.2 * x + 0.5)) / (x * (6.2 * x + 1.7) + 0.06), 1);
    SGDiffuse = lerp(SGDiffuse, DiffuseTonemapping, 1); // 1 is complete toonmapping.

    return SGDiffuse;
}
/* Example
float SG_Curvature = SAMPLE_TEXTURE2D(_SkinBaseMap, sampler_SkinBaseMap, i.texcoord.xy).w;

float SG_Clamp = clamp((SG_Curvature * _SkinScatterAmountMulti + _SkinScatterAmountAdd), _SkinScatterClampMin, _SkinScatterClampMax);
SG_Clamp = pow (SG_Clamp, 5) ;
                
float3 SG_Mid_Result = SGSGDiffuseLighting
(
fin_normal, 
L, 
_SkinScatterAmount * SG_Clamp
);

// #ifdef _SSS_ON
                DirectColor = (diffuse + specular) * SG_Mid_Result * PI * mainLight.color * Shadow;
// #else
//     DirectColor = (diffuse + specular) * NoL * PI * mainLight.color;
// #endif
*/

#endif