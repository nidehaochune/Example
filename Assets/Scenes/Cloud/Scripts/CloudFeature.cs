using Unity.VisualScripting;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

internal enum MyEnum
{
    Cloud,
    Blend
}

public class CloudFeature : ScriptableRendererFeature
{
    private class RayMarchingCloudPass : ScriptableRenderPass
    {
        // public Shader m_cloudShader;
        public Transform cloudTransform;
        private static Mesh s_FullscreenTriangle;

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
                    new(-1f, -1f, 0f),
                    new(-1f, 3f, 0f),
                    new(3f, -1f, 0f)
                });
                s_FullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                s_FullscreenTriangle.UploadMeshData(false);

                return s_FullscreenTriangle;
            }
        }

        private Vector3 boundsMin;
        private Vector3 boundsMax;

        private RTHandle _DownSampleDepthHandle;
        private RTHandle _DownSampleColorHandle;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            UpdateMaterial(cmd, ref renderingData);
            RenderingUtils.ReAllocateIfNeeded(
                ref _DownSampleDepthHandle,
                renderingData.cameraData.cameraTargetDescriptor,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                false, 1, 0,
                "_LowDepthTexture");
            RenderingUtils.ReAllocateIfNeeded(
                ref _DownSampleColorHandle,
                renderingData.cameraData.cameraTargetDescriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                false, 1, 0,
                "_DownsampleColor");
        }

        private void UpdateMaterial(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var projectionMatrix =
                GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, false);
            m_Material.SetMatrix(Shader.PropertyToID("_InverseProjectionMatrix"), projectionMatrix.inverse);
            m_Material.SetMatrix(Shader.PropertyToID("_InverseViewMatrix"),
                renderingData.cameraData.camera.cameraToWorldMatrix);
            m_Material.SetVector(Shader.PropertyToID("_CameraDir"), renderingData.cameraData.camera.transform.forward);

            if (cloudTransform != null)
            {
                boundsMin = cloudTransform.position - cloudTransform.localScale / 2;
                boundsMax = cloudTransform.position + cloudTransform.localScale / 2;
                m_Material.SetVector(Shader.PropertyToID("_boundsMin"), boundsMin);
                m_Material.SetVector(Shader.PropertyToID("_boundsMax"), boundsMax);
            }
            // m_Feature

            if (m_FeatureSetting.cloud3D != null)
                m_Material.SetTexture(Shader.PropertyToID("_noiseTex"), m_FeatureSetting.cloud3D);

            if (m_FeatureSetting.noiseDetail3D != null)
                m_Material.SetTexture(Shader.PropertyToID("_noiseDetail3D"), m_FeatureSetting.noiseDetail3D);

            if (m_FeatureSetting.weatherMap != null)
                m_Material.SetTexture(Shader.PropertyToID("_weatherMap"), m_FeatureSetting.weatherMap);

            if (m_FeatureSetting.maskNoise != null)
                m_Material.SetTexture(Shader.PropertyToID("_maskNoise"), m_FeatureSetting.maskNoise);

            float width = renderingData.cameraData.cameraTargetDescriptor.width;
            float height = renderingData.cameraData.cameraTargetDescriptor.width;

            if (m_FeatureSetting.blueNoise != null)
            {
                var screenUv = new Vector4(
                    width / (float)m_FeatureSetting.blueNoise.width,
                    height / (float)m_FeatureSetting.blueNoise.height, 0, 0);
                m_Material.SetVector(Shader.PropertyToID("_BlueNoiseCoords"), screenUv);
                m_Material.SetTexture(Shader.PropertyToID("_BlueNoise"), m_FeatureSetting.blueNoise);
            }

            m_Material.SetFloat(Shader.PropertyToID("_shapeTiling"), m_FeatureSetting.shapeTiling);
            m_Material.SetFloat(Shader.PropertyToID("_detailTiling"), m_FeatureSetting.detailTiling);

            m_Material.SetFloat(Shader.PropertyToID("_step"), m_FeatureSetting.step);
            m_Material.SetFloat(Shader.PropertyToID("_rayStep"), m_FeatureSetting.rayStep);

            //cmd.SetGlobalFloat(Shader.PropertyToID("_dstTravelled"),m_FeatureSetting.dstTravelled );
            m_Material.SetFloat(Shader.PropertyToID("_densityOffset"), m_FeatureSetting.densityOffset);
            m_Material.SetFloat(Shader.PropertyToID("_densityMultiplier"), m_FeatureSetting.densityMultiplier);


            //cmd.SetInt(Shader.PropertyToID("_numStepsLight"), (int)m_FeatureSetting.numStepsLight );

            m_Material.SetColor(Shader.PropertyToID("_colA"), m_FeatureSetting.colA);
            m_Material.SetColor(Shader.PropertyToID("_colB"), m_FeatureSetting.colB);
            m_Material.SetFloat(Shader.PropertyToID("_colorOffset1"), m_FeatureSetting.colorOffset1);
            m_Material.SetFloat(Shader.PropertyToID("_colorOffset2"), m_FeatureSetting.colorOffset2);
            m_Material.SetFloat(Shader.PropertyToID("_lightAbsorptionTowardSun"),
                m_FeatureSetting.lightAbsorptionTowardSun);
            m_Material.SetFloat(Shader.PropertyToID("_lightAbsorptionThroughCloud"),
                m_FeatureSetting.lightAbsorptionThroughCloud);

            m_Material.SetFloat(Shader.PropertyToID("_rayOffsetStrength"), m_FeatureSetting.rayOffsetStrength);
            m_Material.SetVector(Shader.PropertyToID("_phaseParams"), m_FeatureSetting.phaseParams);
            m_Material.SetVector(Shader.PropertyToID("_xy_Speed_zw_Warp"), m_FeatureSetting.xy_Speed_zw_Warp);
            m_Material.SetVector(Shader.PropertyToID("_shapeNoiseWeights"), m_FeatureSetting.shapeNoiseWeights);
            m_Material.SetFloat(Shader.PropertyToID("_heightWeights"), m_FeatureSetting.heightWeights);
            m_Material.SetFloat(Shader.PropertyToID("_detailWeights"), m_FeatureSetting.detailWeights);
            m_Material.SetFloat(Shader.PropertyToID("_detailNoiseWeight"), m_FeatureSetting.detailNoiseWeight);

            var rotation = Quaternion.Euler(cloudTransform.eulerAngles);
            var scaleMatrix = cloudTransform.localScale * 0.1f;
            scaleMatrix = new Vector3(1 / scaleMatrix.x, 1 / scaleMatrix.y, 1 / scaleMatrix.z);
            var TRSMatrix = Matrix4x4.TRS(cloudTransform.position, rotation, scaleMatrix);
            cmd.SetGlobalMatrix(Shader.PropertyToID("_TRSMatrix"), TRSMatrix);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            var currentTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            //降深度采样
            var DownsampleDepthID = Shader.PropertyToID("_LowDepthTexture");
            cmd.GetTemporaryRT(DownsampleDepthID, renderingData.cameraData.cameraTargetDescriptor, FilterMode.Point);
            BlitFullscreenTriangle(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, DownsampleDepthID,
                0);
            
            // //降cloud分辨率 并使用第1个pass 渲染云
            var DownsampleColorID = Shader.PropertyToID("_DownsampleColor");
            cmd.GetTemporaryRT(DownsampleColorID, renderingData.cameraData.cameraTargetDescriptor, FilterMode.Bilinear);
            BlitFullscreenTriangle(cmd, DownsampleDepthID, DownsampleColorID, 1);
            
            // //使用第2个Pass 合成
            // cmd.SetGlobalTexture(Shader.PropertyToID("_MainTex"), currentTarget);
            BlitFullscreenTriangle(cmd, DownsampleColorID, currentTarget, 2);
            
            // cmd.Blit(DownsampleColorID,renderingData.cameraData.renderer.cameraColorTargetHandle);
            
            
            // cmd.SetGlobalTexture(Shader.PropertyToID("_LowDepthTexture"), _DownSampleDepthHandle.name);
            //
            // Blitter.BlitCameraTexture(cmd, currentTarget, _DownSampleDepthHandle, m_Material, 1);
            //
            //
            // Blitter.BlitCameraTexture(cmd, _DownSampleDepthHandle, _DownSampleColorHandle, m_Material, 0);
            // cmd.SetGlobalTexture(Shader.PropertyToID("_DownsampleColor"), _DownSampleColorHandle);
            //
            // Blitter.BlitCameraTexture(cmd, _DownSampleColorHandle, currentTarget, m_Material, 2);


            context.ExecuteCommandBuffer(cmd);

            // context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 2);

            // cmd.ReleaseTemporaryRT(DownsampleColorID);
            // cmd.ReleaseTemporaryRT(DownsampleDepthID);
            cmd.Clear();
            // cmd.Release();
            CommandBufferPool.Release(cmd);
            // }
        }

        public void MBlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination, int pass, RenderBufferLoadAction loadAction, Rect? viewport = null,
            bool preserveDepth = false)
        {
            // cmd.SetGlobalTexture(Shader.PropertyToID("_MainTex"), source);
            var clear = loadAction == RenderBufferLoadAction.Clear;
            if (clear)
                loadAction = RenderBufferLoadAction.DontCare;

            if (viewport != null)
                loadAction = RenderBufferLoadAction.Load;

            cmd.SetRenderTarget(destination, loadAction, RenderBufferStoreAction.Store, loadAction, depthStoreAction);
            if (viewport != null)
                cmd.SetViewport(viewport.Value);

            if (clear)
                cmd.ClearRenderTarget(true, true, Color.clear);

            cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, m_Material, 0, pass);
        }

        public void BlitFullscreenTriangle(CommandBuffer cmd, RenderTargetIdentifier source,
            RenderTargetIdentifier destination, int pass, bool clear = false,
            Rect? viewport = null, bool preserveDepth = false)
        {
            MBlitFullscreenTriangle(cmd, source, destination, pass,
                clear ? RenderBufferLoadAction.Clear : RenderBufferLoadAction.DontCare, viewport, preserveDepth);
        }
    }

    private RayMarchingCloudPass m_ScriptablePass;

    // public CloudSetting cloudSetting;
    public Texture3D cloud3D;
    public Texture3D noiseDetail3D;

    public float shapeTiling;
    public float detailTiling;

    public Texture2D weatherMap;
    public Texture2D maskNoise;
    public Texture2D blueNoise;

    //light
    //public float numStepsLight = new float { value = 6 };
    public Color colA;
    public Color colB;
    public float colorOffset1;
    public float colorOffset2;
    public float lightAbsorptionTowardSun;
    public float lightAbsorptionThroughCloud;
    public Vector4 phaseParams;

    //density
    public float densityOffset;
    public float densityMultiplier;
    public float step;
    public float rayStep;
    public float rayOffsetStrength;
    [Range(1, 16)] public int Downsample;
    [Range(0, 1)] public float heightWeights;
    public Vector4 shapeNoiseWeights;
    public float detailWeights;
    public float detailNoiseWeight;

    public Vector4 xy_Speed_zw_Warp;

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
            m_ScriptablePass.cloudTransform = cloudTransform;
            renderer.EnqueuePass(m_ScriptablePass);
        }
        else
        {
            findCloudBox = GameObject.Find("CloudBox");
        }

        if (m_ScriptablePass.m_Material == null)
        {
            m_ScriptablePass.m_Material = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Custom/RayMarchingCloud"));
        }
    }
}