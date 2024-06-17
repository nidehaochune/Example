using Unity.VisualScripting;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CloudFeature : ScriptableRendererFeature
{
    class RayMarchingCloudPass : ScriptableRenderPass
    {
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    RayMarchingCloudPass m_ScriptablePass;
    // public CloudSetting cloudSetting;
    public Texture3D cloud3D ;
    public Texture3D noiseDetail3D;

    public float shapeTiling ;
    public float detailTiling ;

    public Texture2D weatherMap;
    public Texture2D maskNoise ;
    public Texture2D blueNoise ;

    //light
    //public float numStepsLight = new float { value = 6 };
    public Color colA ;
    public Color colB ;
    public float colorOffset1 ;
    public float colorOffset2 ;
    public float lightAbsorptionTowardSun ;
    public float lightAbsorptionThroughCloud ;
    public Vector4 phaseParams;

    //density
    public float densityOffset ;
    public float densityMultiplier ;
    public float step ;
    public float rayStep ;
    public float rayOffsetStrength ;
    [Range(1, 16)]
    public int Downsample ;
    [Range(0, 1)]
    public float heightWeights ;
    public Vector4 shapeNoiseWeights ;
    public float detailWeights ;
    public float detailNoiseWeight ;

    public Vector4Parameter xy_Speed_zw_Warp ;

    private GameObject findCloudBox;
    private Transform cloudTransform;
    /// <inheritdoc/>
    public override void Create()
    {
        // cloudSetting = new CloudSetting();
        m_ScriptablePass = new RayMarchingCloudPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

        if (findCloudBox != null)
        {
            cloudTransform = findCloudBox.GetComponent<Transform>();
            renderer.EnqueuePass(m_ScriptablePass);
        }
        else
        {
            findCloudBox = GameObject.Find("CloudBox");
        }
    }
}