using Unity.VisualScripting;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.PostProcessing;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

enum MyEnum
{
    Cloud,
    Blend
}

public class CloudFeature : ScriptableRendererFeature
{
    class RayMarchingCloudPass : ScriptableRenderPass
    {
        // public Shader m_cloudShader;
        public Transform cloudTransform;
        static Mesh s_FullscreenTriangle;

        // public 
        public CloudFeature m_FeatureSetting;
        public Material m_Material;
        public static Mesh fullscreenTriangle
        {
            get
            {
                if (s_FullscreenTriangle != null)
                    return s_FullscreenTriangle;

                s_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };

                // Because we have to support older platforms (GLES2/3, DX9 etc) we can't do all of
                // this directly in the vertex shader using vertex ids :(
                s_FullscreenTriangle.SetVertices(new List<Vector3>
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f,  3f, 0f),
                    new Vector3(3f, -1f, 0f)
                });
                s_FullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                s_FullscreenTriangle.UploadMeshData(false);

                return s_FullscreenTriangle;
            }
        }
        private ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(MyEnum.Cloud);

        
        Vector3 boundsMin;
        Vector3 boundsMax;
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            CommandBuffer cmd = CommandBufferPool.Get();
            // using (new ProfilingScope(cmd, m_ProfilingSampler))
            // {
            // MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, false);
            cmd.SetGlobalMatrix(Shader.PropertyToID("_InverseProjectionMatrix"), projectionMatrix.inverse);
            cmd.SetGlobalMatrix(Shader.PropertyToID("_InverseViewMatrix"), renderingData.cameraData.camera.cameraToWorldMatrix);
            cmd.SetGlobalVector(Shader.PropertyToID("_CameraDir"), renderingData.cameraData.camera.transform.forward);
            
            if (cloudTransform!= null)
            {
                boundsMin = cloudTransform.position - cloudTransform.localScale / 2;
                boundsMax = cloudTransform.position + cloudTransform.localScale / 2;
                cmd.SetGlobalVector(Shader.PropertyToID("_boundsMin"), boundsMin);
                cmd.SetGlobalVector(Shader.PropertyToID("_boundsMax"), boundsMax);
            }
            // m_Feature

            if (m_FeatureSetting.cloud3D != null)
            {
                cmd.SetGlobalTexture(Shader.PropertyToID("_noiseTex"), m_FeatureSetting.cloud3D);
            }
            if (m_FeatureSetting.noiseDetail3D != null)
            {
                cmd.SetGlobalTexture(Shader.PropertyToID("_noiseDetail3D"), m_FeatureSetting.noiseDetail3D);
            }
            if (m_FeatureSetting.weatherMap != null)
            {
                cmd.SetGlobalTexture(Shader.PropertyToID("_weatherMap"), m_FeatureSetting.weatherMap);
            }
            if (m_FeatureSetting.maskNoise != null)
            {
                cmd.SetGlobalTexture(Shader.PropertyToID("_maskNoise"), m_FeatureSetting.maskNoise);
            }

            float width = renderingData.cameraData.cameraTargetDescriptor.width;
            float height = renderingData.cameraData.cameraTargetDescriptor.width;

            if (m_FeatureSetting.blueNoise  != null)
            {
                Vector4 screenUv = new Vector4(
                width / (float)m_FeatureSetting.blueNoise .width,
                height / (float)m_FeatureSetting.blueNoise .height,0,0);
                cmd.SetGlobalVector(Shader.PropertyToID("_BlueNoiseCoords"), screenUv);
                cmd.SetGlobalTexture(Shader.PropertyToID("_BlueNoise"), m_FeatureSetting.blueNoise );
            }
            
            cmd.SetGlobalFloat(Shader.PropertyToID("_shapeTiling"), m_FeatureSetting.shapeTiling );
            cmd.SetGlobalFloat(Shader.PropertyToID("_detailTiling"), m_FeatureSetting.detailTiling );
    
            cmd.SetGlobalFloat(Shader.PropertyToID("_step"), m_FeatureSetting.step );
            cmd.SetGlobalFloat(Shader.PropertyToID("_rayStep"), m_FeatureSetting.rayStep );
    
            //cmd.SetGlobalFloat(Shader.PropertyToID("_dstTravelled"),m_FeatureSetting.dstTravelled );
            cmd.SetGlobalFloat(Shader.PropertyToID("_densityOffset"), m_FeatureSetting.densityOffset );
            cmd.SetGlobalFloat(Shader.PropertyToID("_densityMultiplier"), m_FeatureSetting.densityMultiplier );
    
            
            //cmd.SetInt(Shader.PropertyToID("_numStepsLight"), (int)m_FeatureSetting.numStepsLight );
    
            cmd.SetGlobalColor(Shader.PropertyToID("_colA"), m_FeatureSetting.colA );
            cmd.SetGlobalColor(Shader.PropertyToID("_colB"), m_FeatureSetting.colB );
            cmd.SetGlobalFloat(Shader.PropertyToID("_colorOffset1"), m_FeatureSetting.colorOffset1 );
            cmd.SetGlobalFloat(Shader.PropertyToID("_colorOffset2"), m_FeatureSetting.colorOffset2 );
            cmd.SetGlobalFloat(Shader.PropertyToID("_lightAbsorptionTowardSun"), m_FeatureSetting.lightAbsorptionTowardSun );
            cmd.SetGlobalFloat(Shader.PropertyToID("_lightAbsorptionThroughCloud"), m_FeatureSetting.lightAbsorptionThroughCloud );
    
            
            cmd.SetGlobalFloat(Shader.PropertyToID("_rayOffsetStrength"), m_FeatureSetting.rayOffsetStrength );
            cmd.SetGlobalVector(Shader.PropertyToID("_phaseParams"), m_FeatureSetting.phaseParams );
            cmd.SetGlobalVector(Shader.PropertyToID("_xy_Speed_zw_Warp"), m_FeatureSetting.xy_Speed_zw_Warp );
            
            cmd.SetGlobalVector(Shader.PropertyToID("_shapeNoiseWeights"), m_FeatureSetting.shapeNoiseWeights );
            cmd.SetGlobalFloat(Shader.PropertyToID("_heightWeights"), m_FeatureSetting.heightWeights );
    
            
            cmd.SetGlobalFloat(Shader.PropertyToID("_detailWeights"), m_FeatureSetting.detailWeights );
            cmd.SetGlobalFloat(Shader.PropertyToID("_detailNoiseWeight"), m_FeatureSetting.detailNoiseWeight );

            Quaternion rotation = Quaternion.Euler(cloudTransform.eulerAngles);
            Vector3 scaleMatrix = cloudTransform.localScale * 0.1f;
            scaleMatrix = new Vector3(1 / scaleMatrix.x, 1 / scaleMatrix.y, 1 / scaleMatrix.z);
            Matrix4x4 TRSMatrix = Matrix4x4.TRS(cloudTransform.position, rotation, scaleMatrix);
            cmd.SetGlobalMatrix(Shader.PropertyToID("_TRSMatrix"), TRSMatrix);
      
                
                //降深度采样
                var DownsampleDepthID = Shader.PropertyToID("_DownsampleTemp");
                cmd.GetTemporaryRT(DownsampleDepthID,renderingData.cameraData.cameraTargetDescriptor,FilterMode.Point);
                BlitFullscreenTriangle(cmd,renderingData.cameraData.renderer.cameraColorTargetHandle, DownsampleDepthID, 1);
                cmd.SetGlobalTexture(Shader.PropertyToID("_LowDepthTexture"), DownsampleDepthID);


                // //降cloud分辨率 并使用第1个pass 渲染云
                var DownsampleColorID = Shader.PropertyToID("_DownsampleColor");
                cmd.GetTemporaryRT( DownsampleColorID,renderingData.cameraData.cameraTargetDescriptor,FilterMode.Bilinear);
                BlitFullscreenTriangle(cmd,DownsampleDepthID, DownsampleColorID, 0);

                // //降分辨率后的云设置回_DownsampleColor
                cmd.SetGlobalTexture(Shader.PropertyToID("_DownsampleColor"), DownsampleColorID);
                // //使用第2个Pass 合成
                BlitFullscreenTriangle(cmd, DownsampleColorID,renderingData.cameraData.renderer.cameraColorTargetHandle, 2);
                
                // cmd.Blit(DownsampleColorID,renderingData.cameraData.renderer.cameraColorTargetHandle);

                context.ExecuteCommandBuffer(cmd);
                
                // context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 2);
       
                cmd.ReleaseTemporaryRT(DownsampleColorID);
                cmd.ReleaseTemporaryRT(DownsampleDepthID);
                cmd.Clear();
                // cmd.Release();
                CommandBufferPool.Release(cmd);
            // }

        }
        public void MBlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, int pass, RenderBufferLoadAction loadAction, Rect? viewport = null, bool preserveDepth = false)
        {
            cmd.SetGlobalTexture(Shader.PropertyToID("_MainTex"), source);
            bool clear = (loadAction == RenderBufferLoadAction.Clear);
            if (clear)
                loadAction = RenderBufferLoadAction.DontCare;

            if (viewport != null)
                loadAction = RenderBufferLoadAction.Load;

            // cmd.SetRenderTargetWithLoadStoreAction(destination, loadAction, RenderBufferStoreAction.Store, preserveDepth ? RenderBufferLoadAction.Load : loadAction, RenderBufferStoreAction.Store);
            cmd.SetRenderTarget(destination, loadAction, RenderBufferStoreAction.Store, loadAction, depthStoreAction);
            if (viewport != null)
                cmd.SetViewport(viewport.Value);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, m_Material, 0, pass);
        }
        public void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination,  int pass, bool clear = false,
            Rect? viewport = null, bool preserveDepth = false)
        {
            MBlitFullscreenTriangle(cmd,source, destination, pass,
                clear ? RenderBufferLoadAction.Clear : RenderBufferLoadAction.DontCare, viewport, preserveDepth);
            
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

    public Vector4 xy_Speed_zw_Warp ;

    private GameObject findCloudBox;
    private Transform cloudTransform;
    private Material Material;

    /// <inheritdoc/>
    public override void Create()
    {
        // cloudSetting = new CloudSetting();
        m_ScriptablePass = new RayMarchingCloudPass();
        m_ScriptablePass.m_FeatureSetting = this;
        Material = new Material(Shader.Find("Hidden/Custom/RayMarchingCloud"));
        m_ScriptablePass.m_Material = Material;
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

        if (findCloudBox != null)
        {
            cloudTransform = findCloudBox.GetComponent<Transform>();
            m_ScriptablePass.cloudTransform = this.cloudTransform;

            renderer.EnqueuePass(m_ScriptablePass);
        }
        else
        {
            findCloudBox = GameObject.Find("CloudBox");
        }
    }
}