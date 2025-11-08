#if GLASSSHADER_USING_HDRP
using System;
using System.Collections.Generic;
using GlassShader.CPURenderPass;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;



namespace GlassShader.HDRP
{
    public class GlassRenderPass : CustomPass
    {
        public static GlassRenderPass ins;
        private RTHandle RTHandle_cameraCopy;
        private RTHandle RTHandle_PassOutputPing;
        private RTHandle RTHandle_ActualCameraBuffer;
        private RTHandle BlankTexture;
        [Header("Compute shader setup")] 
        public ComputeShader ComputeShader_CaculateOutputImage;
        public ComputeShader ComputeShader_ComputeBlurPass;

        private bool setUPFlag;
        private RTHandle resultRT;
        //public Material HDRPBlitCorrectTexture;
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (ins != null)
            {
                Debug.LogError("Finding two instances for the GlassRenderPass. Please delete one");
            }
            ins = this;
            setUPFlag = true;
            // Allocate temp RT for the camera color 
            Debug.Log("MyDrawRendererListPass.Setup");
    
            RTHandle_ActualCameraBuffer = RTHandles.Alloc(
                Vector2.one, 1, dimension: TextureDimension.Tex2D,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat, //
                useDynamicScale: true, name: "_ActualCameraBuffer"
            );
            BlankTexture = RTHandles.Alloc(
                Vector2.one, 1, dimension: TextureDimension.Tex2D,
                colorFormat: GraphicsFormat.R16G16B16A16_SFloat, //
                useDynamicScale: true, name: "_BlankTexture"
            );
            
        }
    
        int SetupBlurPass(CommandBuffer cmd, Material renderObjectmaterial)
        {
            var LightTexture = renderObjectmaterial.GetTexture(ShaderID.LightTexture);
            var LightTextureContrast = renderObjectmaterial.GetFloat(ShaderID.LightTextureContrast);
            var LightTextureBrightness = renderObjectmaterial.GetFloat(ShaderID.LightTextureBrightness);
            // Get kernel id
            var ComputeShaderKernel = ComputeShader_ComputeBlurPass.FindKernel("BlurPass");
            // Set compute shader properties
        
            if (LightTexture != null)
                cmd.SetComputeTextureParam(ComputeShader_ComputeBlurPass, ComputeShaderKernel,ShaderID.LightTexture,LightTexture);
            else
            {
                cmd.SetComputeTextureParam(ComputeShader_ComputeBlurPass, ComputeShaderKernel,ShaderID.LightTexture,BlankTexture);
            }
            cmd.SetComputeFloatParam(ComputeShader_ComputeBlurPass,ShaderID.LightTextureContrast, LightTextureContrast);
            cmd.SetComputeFloatParam(ComputeShader_ComputeBlurPass,ShaderID.LightTextureBrightness, LightTextureBrightness);
            return ComputeShaderKernel;
       
        }
        void ComputeBlurPass(CommandBuffer cmd, int kernelIndex,int width,int height)
        {
            uint tx, ty, tz;
            ComputeShader_ComputeBlurPass.GetKernelThreadGroupSizes(kernelIndex, out tx, out ty, out tz);
            cmd.DispatchCompute(ComputeShader_ComputeBlurPass, kernelIndex, Mathf.CeilToInt(width / tx) ,Mathf.CeilToInt(height / ty),1);
        }

    
        // public Material overrideMaterial;
        public List<InputMaterialAndShaderTag> RenderObjects = new List<InputMaterialAndShaderTag>();
        protected override void Execute(CustomPassContext ctx)
        {
      
            if (ctx.hdCamera.camera.cameraType == CameraType.SceneView)
                return;
            if (ComputeShader_CaculateOutputImage == null || 
                ComputeShader_ComputeBlurPass == null)
            {
                Debug.LogError("Please set the compute shader");
                return;
            }

            int count = 0;
            foreach (var renderObject in RenderObjects)
            {
                if (!renderObject.container || !renderObject.enable )
                    continue;
                //Render(ctx,renderObject.container.GlassMaterial, renderObject.container.hashID,renderObject.layerMask);//
                RenderGlassObject(ctx, renderObject,count);
                count++;
            }
            ctx.cmd.SetRenderTarget( RTHandle_PassOutputPing);
            CoreUtils.ClearRenderTarget(ctx.cmd,ClearFlag.Color ,absoluteZero );
        }
        int prevWidth = 0, prevHeight = 0;
        void CheckForCameraChangeAndCreateTexture(HDCamera camera)
        {
            int width = camera.actualWidth;
            int height = camera.actualHeight;
            camera.camera.GetInstanceID();
            if (width != prevWidth || height != prevHeight || setUPFlag)
            {
                prevWidth = width;
                prevHeight = height;
            
                RTHandle_cameraCopy?.Release();
                RTHandle_cameraCopy = RTHandles.Alloc(
                    width, height, 1, dimension: TextureDimension.Tex2D,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat, //
                    useDynamicScale: true, name: "_CameraColorCopyRT"
            
                );
                RTHandle_PassOutputPing?.Release();
                RTHandle_PassOutputPing =  RTHandles.Alloc(
                    width, height, 1, dimension: TextureDimension.Tex2D,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                    useDynamicScale: true, name: "_Pass0OutputPing",
                    enableRandomWrite:true
                );
                setUPFlag = false;
            }
       
        }
        void CopyCameraTexture(CustomPassContext ctx,RTHandle output,int count)
        {
            // Allocate RT with same size/format as the camera color buffer
            if (count == 0)
                Blitter.BlitCameraTexture(ctx.cmd,ctx.cameraColorBuffer,output);
            // else 
            //     Blitter.BlitCameraTexture(ctx.cmd,render,output);

        }

    
        Color absoluteZero = new Color(0, 0, 0, 0);
        void RenderGlassObject(CustomPassContext ctx, InputMaterialAndShaderTag renderObject,int count)
        {
       
            CheckForCameraChangeAndCreateTexture(ctx.hdCamera);
            Camera camera = ctx.hdCamera.camera;
            float RealCamW = ctx.hdCamera.actualWidth;
            float RealCamH = ctx.hdCamera.actualHeight;
            float RateCamvsCameraBufferW = RealCamW / ctx.cameraColorBuffer.rt.width;
            float RateCamvsCameraBufferH = RealCamH / ctx.cameraColorBuffer.rt.height;
       
            renderObject.container.GlassMaterial.SetVector(ShaderID.RateCamvsCamBuffer,new Vector4(RateCamvsCameraBufferW,RateCamvsCameraBufferH,1,1));
            // if (camera.cameraType == CameraType.SceneView)
            //     return;
            // Take the cam render texture and put it into the 0th pass
            CopyCameraTexture(ctx,RTHandle_cameraCopy,count);
            CopyCameraTexture(ctx,RTHandle_ActualCameraBuffer,count);
            ctx.cmd.SetGlobalTexture(ShaderID.GlobalTexture,RTHandle_cameraCopy);
            // Render first 

            Render(ctx,renderObject, RTHandle_PassOutputPing,0,ShaderID.ShaderPassOutput);
            var desc =  RTHandle_PassOutputPing.rt.descriptor;
            int kernelBlurPass = SetupBlurPass(ctx.cmd,renderObject.container.GlassMaterial);
            // alloc the new rttexture
            int blurI = renderObject.container.GlassMaterial.GetInteger(ShaderID.BlurIntensity);
            ctx.cmd.SetComputeVectorParam(ComputeShader_ComputeBlurPass,ShaderID.LightTextureTilingAndoffset,Utility.GetLightTextureScale(renderObject.container.GlassMaterial));
            ctx.cmd.SetComputeTextureParam(ComputeShader_ComputeBlurPass,kernelBlurPass, ShaderID.InputComputeShaderBlurPass, RTHandle_PassOutputPing);
            ctx.cmd.SetComputeTextureParam(ComputeShader_ComputeBlurPass,kernelBlurPass, ShaderID.ComputeShaderResult, RTHandle_PassOutputPing);
            for (int i = 0; i < blurI; i++)
            {
            
                ComputeBlurPass(ctx.cmd,kernelBlurPass, desc.width,desc.height);
                //SetupBlurPass(renderObject.container.GlassMaterial,resultRT,resultRT);
                // Render(ctx,renderObject,PassOutput,1,ShaderID.ShaderPassOutput);
  
            }
            ctx.cmd.SetGlobalTexture(ShaderID.ShaderPassOutput, RTHandle_PassOutputPing);
            Render(ctx,renderObject,RTHandle_cameraCopy,2);
       
        
            // Blit back into the screen
            desc = ctx.cameraColorBuffer.rt.descriptor;
            var resultRT = RTHandles.Alloc(desc.width, desc.height, colorFormat: GraphicsFormat.R16G16B16A16_SFloat,enableRandomWrite:true,name:"_Copytocameratexture");

            int kernel = ComputeShader_CaculateOutputImage.FindKernel("CSMain");
            ComputeShader_CaculateOutputImage.SetTexture(kernel,ShaderID.ComputeShader_CalcOutput_Input,RTHandle_cameraCopy);
            ComputeShader_CaculateOutputImage.SetTexture(kernel,ShaderID.ComputeShader_CalcOutput_CameraActualTexture,RTHandle_ActualCameraBuffer);
            ComputeShader_CaculateOutputImage.SetTexture(kernel,ShaderID.ComputeShaderResult,resultRT);
            // Get the dim of the colorbuffer
            // Get the output of the compute shader
    
            ctx.cmd.SetComputeTextureParam(ComputeShader_CaculateOutputImage,kernel,ShaderID.ComputeShaderResult,resultRT);
            // Run the compute shader 
            ctx.cmd.DispatchCompute(ComputeShader_CaculateOutputImage,kernel,(desc.width / 8),(desc.height / 8),1 );
     
            ctx.cmd.Blit(resultRT,ctx.cameraColorBuffer);
      

            //
            // Release the output of compute shader
            resultRT.Release();
        }

    
        void Render(CustomPassContext ctx,InputMaterialAndShaderTag renderObject, RTHandle output, int pass = 0,int OutputID  = 0)
        {
            ctx.cmd.SetRenderTarget(output);
        
            if (!renderObject.container.GlassMaterial || String.IsNullOrEmpty(renderObject.container.hashID))
            {
                return;
            }
        
            var rendererListDesc = new RendererListDesc(
                new ShaderTagId(renderObject.container.hashID),          // Pass name to match in shaders
                ctx.cullingResults,
                ctx.hdCamera.camera
            )
            {
                sortingCriteria = SortingCriteria.CommonTransparent,
                rendererConfiguration = PerObjectData.LightData | PerObjectData.Lightmaps | PerObjectData.MotionVectors,
                renderQueueRange = RenderQueueRange.all,
                layerMask = renderObject .layerMask,
                overrideMaterial = renderObject.container.GlassMaterial,
                overrideMaterialPassIndex = pass,
          
            };
//        var UIoverlay = ctx.renderContext.CreateUIOverlayRendererList(ctx.hdCamera.camera,UISubset.LowLevel);
            var rendererList = ctx.renderContext.CreateRendererList(rendererListDesc);
        
            CoreUtils.DrawRendererList(ctx.renderContext,ctx.cmd ,rendererList);
            if (OutputID != 0)
                renderObject.container.GlassMaterial.SetTexture(OutputID, output);
        }

        protected override void Cleanup()
        { 
            RTHandle_cameraCopy?.Release();
            RTHandle_PassOutputPing?.Release();
            BlankTexture?.Release();
            RTHandle_ActualCameraBuffer?.Release();
        }
    }
}
#endif