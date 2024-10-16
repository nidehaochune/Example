using System;
using System.Collections;
using System.Dynamic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.XR.XRDisplaySubsystem;


public class DepthPyramid : ScriptableRendererFeature
{
    const int mBUFFERSIZE = 11;

    class DepthPyramidPass : ScriptableRenderPass
    {
        const int mTHREADS = 8;

        internal Settings settings { get; }

        internal struct TargetSlice
        {
            internal int slice;
            internal Vector2Int paddedResolution;
            internal Vector2Int actualResolution;
            internal Vector2 scale;

            public static implicit operator int(TargetSlice target)
            {
                return target.slice;
            }
        }

        public DepthPyramidPass(ComputeBuffer depthSliceBuffer, Settings settings)
        {
            depthSliceResolutions = depthSliceBuffer;
            this.settings = settings;
        }

        int finalDepthPyramidID;
        TargetSlice[] tempSlices = new TargetSlice[mBUFFERSIZE];
        Vector2Int[] sliceResolutions = new Vector2Int[mBUFFERSIZE];
        ComputeBuffer depthSliceResolutions = null;

        Vector2 screenSize;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            //depthSliceResolutions = new ComputeBuffer(buffersize, sizeof(int) * 2, ComputeBufferType.Default);
            int width = (int)(renderingData.cameraData.cameraTargetDescriptor.width *
                              GlobalSSRSettings.GlobalResolutionScale);
            int height = (int)(renderingData.cameraData.cameraTargetDescriptor.height *
                               GlobalSSRSettings.GlobalResolutionScale);

            int paddedWidth = Mathf.NextPowerOfTwo(width);
            int paddedHeight = Mathf.NextPowerOfTwo(height);

            screenSize.x = paddedWidth;
            screenSize.y = paddedHeight;

            for (int i = 0; i < mBUFFERSIZE; i++)
            {
                tempSlices[i].paddedResolution.x = Mathf.Max(paddedWidth >> i, 1);
                tempSlices[i].paddedResolution.y = Mathf.Max(paddedHeight >> i, 1);

                tempSlices[i].actualResolution.x = Mathf.CeilToInt(width / (i + 1));
                tempSlices[i].actualResolution.y = Mathf.CeilToInt(height / (i + 1));

                tempSlices[i].slice = i;

                tempSlices[i].scale.x = tempSlices[i].paddedResolution.x / (float)paddedWidth;
                tempSlices[i].scale.y = tempSlices[i].paddedResolution.y / (float)paddedHeight;
                sliceResolutions[i] = tempSlices[i].paddedResolution;

                //tempScale[i] = tempSlices[i].scale;
                //Debug.Log(tempSlices[i].resolution + "_x" + tempSlices[i].scale);
                //Debug.Log(tempSlices[i].resolution);
            }

            finalDepthPyramidID = Shader.PropertyToID("_DepthPyramid");
            depthSliceResolutions.SetData(sliceResolutions);
            Shader.SetGlobalBuffer("_DepthPyramidResolutions", depthSliceResolutions);

#if UNITY_2022_1_OR_NEWER
            ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle,
                renderingData.cameraData.renderer.cameraColorTargetHandle);
#else
                ConfigureTarget(renderingData.cameraData.renderer.cameraColorTarget, renderingData.cameraData.renderer.cameraDepthTarget);
#endif
        }

        void SetComputeShader(CommandBuffer cmd, RenderTargetIdentifier tArray, int sSlice, int dSlice, int sW, int sH,
            int dW, int dH)
        {
            cmd.SetComputeTextureParam(settings.shader, 0, "source", tArray);
            cmd.SetComputeVectorParam(settings.shader, "sSize", new Vector2(sW, sH));
            cmd.SetComputeVectorParam(settings.shader, "dSize", new Vector2(dW, dH));
            cmd.SetComputeIntParam(settings.shader, "sSlice", sSlice);
            cmd.SetComputeIntParam(settings.shader, "dSlice", dSlice);

            //bool extraX = !Mathf.IsPowerOfTwo(dW);
            //bool extraY = !Mathf.IsPowerOfTwo(dH);

            //cmd.SetComputeIntParam(settings.shader, "extraSampleX", extraX ? 1 : 0);
            //cmd.SetComputeIntParam(settings.shader, "extraSampleY", extraY ? 1 : 0);
        }

        void SetDebugComputeShader(CommandBuffer cmd, RenderTargetIdentifier source, int slice, float low, float high)
        {
            cmd.SetComputeTextureParam(settings.shader, 2, "source", source);
            cmd.SetComputeTextureParam(settings.shader, 3, "source", source);
            cmd.SetComputeTextureParam(settings.shader, 4, "source", source);
            cmd.SetComputeFloatParam(settings.shader, "low", low);
            cmd.SetComputeFloatParam(settings.shader, "high", high);
            cmd.SetComputeIntParam(settings.shader, "dSlice", slice);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            float width = screenSize.x;
            float height = screenSize.y;

            float actualWidth = renderingData.cameraData.cameraTargetDescriptor.width *
                                GlobalSSRSettings.GlobalResolutionScale;
            float actualHeight = renderingData.cameraData.cameraTargetDescriptor.height *
                                 GlobalSSRSettings.GlobalResolutionScale;
            if (settings.shader == null)
            {
                return;
            }

            {
                var cmd = CommandBufferPool.Get("Init Depth Pyramid");
                cmd.GetTemporaryRTArray(finalDepthPyramidID, (int)width, (int)height, mBUFFERSIZE, 0, FilterMode.Point,
                    RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear, 1, true);
                cmd.SetComputeTextureParam(settings.shader, 1, "source", finalDepthPyramidID);
                cmd.SetComputeVectorParam(settings.shader, "screenSize", new Vector2(actualWidth, actualHeight));
                cmd.DispatchCompute(settings.shader, 1, Mathf.CeilToInt(actualWidth / mTHREADS),
                    Mathf.CeilToInt(actualHeight / mTHREADS), 1);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            {
                var cmd = CommandBufferPool.Get("Calculate Depth Pyramid");
                cmd.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
                for (int i = 0; i < mBUFFERSIZE - 1; i++)
                {
                    //calculate high z depth for the next scaled down buffer
                    SetComputeShader(cmd,
                        finalDepthPyramidID,
                        tempSlices[i],
                        tempSlices[i + 1],
                        tempSlices[i].paddedResolution.x,
                        tempSlices[i].paddedResolution.y,
                        tempSlices[i + 1].paddedResolution.x,
                        tempSlices[i + 1].paddedResolution.y
                    );

                    int xGroup = Mathf.CeilToInt((float)tempSlices[i + 1].actualResolution.x / mTHREADS);
                    int yGroup = Mathf.CeilToInt((float)tempSlices[i + 1].actualResolution.y / mTHREADS);
                    cmd.DispatchCompute(settings.shader, 0, xGroup, yGroup, 1);
                }

                context.ExecuteCommandBufferAsync(cmd, ComputeQueueType.Background);
                CommandBufferPool.Release(cmd);
            }


#if UNITY_EDITOR
            if (settings.ShowDebug)
            {
                var cmd = CommandBufferPool.Get("Debug Depth Pyramid");
                int debug = Mathf.Clamp(settings.DebugSlice, 0, mBUFFERSIZE - 1);

                SetDebugComputeShader(cmd, finalDepthPyramidID, debug, settings.DebugMinMax.x, settings.DebugMinMax.y);
                int cx = Mathf.CeilToInt(width / mTHREADS);
                int cy = Mathf.CeilToInt(height / mTHREADS);
                cmd.DispatchCompute(settings.shader, 2, cx, cy, 1);
                cmd.DispatchCompute(settings.shader, 3, cx, cy, 1);
                cmd.DispatchCompute(settings.shader, 4, cx, cy, 1);

                cmd.Blit(finalDepthPyramidID, colorAttachmentHandle, Vector2.one, Vector2.zero, 0, 0);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
#endif
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            base.OnCameraCleanup(cmd);
            cmd.ReleaseTemporaryRT(finalDepthPyramidID);
        }
    }

    [System.Serializable]
    internal struct Settings
    {
        [HideInInspector] internal ComputeShader shader;
        [SerializeField] internal bool ShowDebug;

        [Range(0, mBUFFERSIZE - 1)] [SerializeField]
        internal int DebugSlice;

        [SerializeField] internal Vector2 DebugMinMax;
    }


    [SerializeField] internal ComputeShader depthPyramidShader;
    Settings settings = new Settings();
    DepthPyramidPass m_ScriptablePass = null;
    ComputeBuffer depthSliceBuffer = null;

    void ReleaseSliceBuffer()
    {
        if (depthSliceBuffer != null)
        {
            depthSliceBuffer.Release();
            depthSliceBuffer = null;
        }
    }

    void CreateSliceBuffer()
    {
        if (depthSliceBuffer == null)
        {
            depthSliceBuffer = new ComputeBuffer(mBUFFERSIZE, sizeof(int) * 2, ComputeBufferType.Default);
        }
    }

    /// <inheritdoc/>
    public override void Create()
    {
        ReleaseSliceBuffer();
        CreateSliceBuffer();
        m_ScriptablePass = new DepthPyramidPass(depthSliceBuffer, settings);
        settings.shader = depthPyramidShader;
        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;
        if (settings.ShowDebug)
        {
            m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }
    }

    private void OnDestroy()
    {
        ReleaseSliceBuffer();
    }

    private void OnDisable()
    {
        ReleaseSliceBuffer();
    }

    private void OnValidate()
    {
        ReleaseSliceBuffer();
        CreateSliceBuffer();
        Create();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!renderingData.cameraData.postProcessEnabled)
        {
            return;
        }

        settings.shader = depthPyramidShader;
#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
        var d = UnityEngine.Rendering.Universal.UniversalRenderPipelineDebugDisplaySettings.Instance
            .AreAnySettingsActive;
        if (!d)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
#else
            renderer.EnqueuePass(m_ScriptablePass);
#endif
    }
}