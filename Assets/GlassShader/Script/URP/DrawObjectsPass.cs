#if GLASSSHADER_USING_URP
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace GlassShader.CPURenderPass
{

    public class DrawObjectsPass : ScriptableRenderPass
    {

        //
        // public Material useMaterial;
        // public String ShaderTag;
        public ComputeShader ComputeShader_BlurPass;
        public LayerMask LayerMask = ~0;
        public List<InputMaterialAndShaderTag> ListRenderObjects;
        //
        public DrawObjectsPass(List<InputMaterialAndShaderTag> listRenderObjects,LayerMask layerMask,ComputeShader blurpass)
        {
            // Set the pass's local copy of the override material 
            this.ListRenderObjects = listRenderObjects;
            this.LayerMask = layerMask;
            this.ComputeShader_BlurPass = blurpass; 
            requiresIntermediateTexture = true;
            

        }
       
        private class PassData
        {
            public DrawingSettings drawSettings;
            public Material material;
            internal bool useMSAA;
            // Create a field to store the list of objects to draw
            public RendererListHandle rendererListHandle;
        }



        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            
            RenderPipelineAsset currentAsset  = GraphicsSettings.currentRenderPipeline;
            if (currentAsset != null)
            {
                UniversalRenderPipelineAsset URPRenderasset = currentAsset as UniversalRenderPipelineAsset;

            }
    
			
            UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
            var desc = cameraData.cameraTargetDescriptor;
            desc.sRGB = false;
            desc.depthBufferBits = 0;
            desc.enableRandomWrite = true;
            
            desc.msaaSamples = 1;
            int tempMsaa = desc.msaaSamples;
            var GlobalTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph,desc,"GlobalTexure",false);
           foreach (var item in ListRenderObjects)
           {
               if (item.container == null || !item.enable)
                   continue;
               item.OutputPass = UniversalRenderer.CreateRenderGraphTexture(renderGraph,desc,"_OutputPass",false);
                
               BlitCameraTexturePass.CopyColorAndOutGlobalTex(renderGraph,GlobalTexture,resourceData,ShaderID.GlobalTexture);
               RenderObject(item.container.GlassMaterial,item.container.hashID,  item.OutputPassNoMsaa,item.OutputPass, renderGraph, frameContext,
                   cameraData, resourceData);
           }
            
           // 
        
        }
        
        void RenderObject(Material materialToUse,String LightModetag,TextureHandle outputTextureNoMsaa, TextureHandle outputTexture,
                            RenderGraph renderGraph,ContextContainer frameContext,UniversalCameraData cameraData,UniversalResourceData resourceData)
        {
            
           
             int blurIntensity = materialToUse.GetInteger( ShaderID.BlurIntensity);
    
            int id = Shader.PropertyToID("_GlobalTexture");
    
            //
            RenderAction( materialToUse,LightModetag,"_Pass0Output",outputTexture,0,renderGraph,frameContext,cameraData,resourceData);
            
            if (blurIntensity <= 1)
            {
                Debug.LogError("[Glass Shader] Render glass might not working properly. Please set blur intensity > 1");
            }
            else
            for (int i = 0; i < blurIntensity; i++)
            {
                var lightParam = new LightTextureParam(materialToUse);
                ComputeBlurAction(renderGraph,frameContext,outputTexture,lightParam,i == blurIntensity - 1);
            }
   
            RenderAction(materialToUse,LightModetag,"",resourceData.activeColorTexture,2,renderGraph,frameContext,cameraData,resourceData);
        }
        void RenderAction(Material materialToUse,String LightMode, string OutputName,TextureHandle output,int pass,
                        RenderGraph renderGraph,ContextContainer frameContext,UniversalCameraData cameraData,UniversalResourceData resourceData)
        {
               using (var builder = renderGraph.AddRasterRenderPass<PassData>("Redraw objects", out var passData))
            {
                // Get the data needed to create the list of objects to draw
                UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
               
                UniversalLightData lightData = frameContext.Get<UniversalLightData>();
                SortingCriteria sortFlags = SortingCriteria.None;
                RenderQueueRange renderQueueRange = RenderQueueRange.all;
                FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, LayerMask);
    
                // Redraw only objects that have their LightMode tag set to UniversalForward 
                ShaderTagId shadersToOverride = new ShaderTagId(LightMode);
                
                // Create drawing settings
                DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, sortFlags);
                // Add the override material to the drawing settings
                drawSettings.overrideMaterial = materialToUse;
                drawSettings.overrideMaterialPassIndex = pass;
                
                // Create the list of objects to draw
                var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

                //// Convert the list to a list handle that the render graph system can use
                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
                passData.material = materialToUse;
     
                // Set the render target as the color and depth textures of the active camera texture
                
               // builder.SetInputAttachment(resourceData.activeColorTexture,0,AccessFlags.Read);
                builder.UseRendererList(passData.rendererListHandle);
                builder.SetRenderAttachment(output, 0);
             //   builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
                
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
               //// Check for export
                // if (OutputName != "")
                //     builder.SetGlobalTextureAfterPass(output, Shader.PropertyToID(OutputName));
            }
            
           
        }

        class LightTextureParam
        {
           
            public Texture LigthTexture;
            public float LightTextureContrast;
            public float LightTextureBrightness;
            public Vector4 ScaleAndOffset;
            public LightTextureParam(Material materialToUse)
            {
                LigthTexture =  materialToUse.GetTexture(ShaderID.LightTexture);
                LightTextureBrightness = materialToUse.GetFloat(ShaderID.LightTextureBrightness);
                LightTextureContrast = materialToUse.GetFloat(ShaderID.LightTextureContrast);
                ScaleAndOffset = Utility.GetLightTextureScale(materialToUse);
            }
            
        }
        class BlurPassData
        {
            public ComputeShader ComputeShader_BlurPass;
            public TextureHandle InpOutTexture;
            public TextureHandle LightTexture;
            public int kernel;
            public LightTextureParam LightData;
            public int width;
            public int height;
            public bool init;
            public uint ThreadX,ThreadY,ThreadZ;
        }
        void ComputeBlurAction(RenderGraph renderGraph,ContextContainer frameContext,  TextureHandle _InputTexture,LightTextureParam lightParam,bool final)
        {
            if (ComputeShader_BlurPass == null)
            {
                Debug.LogError("[Glass Shader] ComputeShader_BlurPass is null, Please set it");
                return;
            }
            using (var builder = renderGraph.AddComputePass<BlurPassData>("Blur pass", out var passData))
            {
                builder.UseTexture(_InputTexture,AccessFlags.ReadWrite);
                
                passData.ComputeShader_BlurPass =  ComputeShader_BlurPass;
                passData.kernel = ComputeShader_BlurPass.FindKernel("BlurPass");
                passData.LightData = lightParam;
                passData.InpOutTexture =  _InputTexture;
                var desc = _InputTexture.GetDescriptor(renderGraph);
                TextureHandle textureHandle_LightTexture;
                if (passData.LightData.LigthTexture)
                {
                    textureHandle_LightTexture = renderGraph.ImportTexture( RTHandles.Alloc(passData.LightData.LigthTexture));
                   
                }
                else
                    textureHandle_LightTexture = renderGraph.CreateTexture(desc);
                passData.LightTexture = textureHandle_LightTexture;
                builder.UseTexture(passData.LightTexture,AccessFlags.ReadWrite);
                
                passData.width = desc.width;
                passData.height = desc.height;
            
                ComputeShader_BlurPass.GetKernelThreadGroupSizes(passData.kernel,out passData.ThreadX,out passData.ThreadY,out passData.ThreadZ);
                // get input desciption
                builder.SetRenderFunc((BlurPassData passData, ComputeGraphContext ctx) =>
                {
                    
                    ctx.cmd.SetComputeVectorParam(passData.ComputeShader_BlurPass,ShaderID.LightTextureTilingAndoffset,passData.LightData.ScaleAndOffset);
                    ctx.cmd.SetComputeTextureParam(passData.ComputeShader_BlurPass,passData.kernel,ShaderID.InputComputeShaderBlurPass, passData.InpOutTexture );
                    ctx.cmd.SetComputeTextureParam(passData.ComputeShader_BlurPass,passData.kernel,ShaderID.LightTexture,passData.LightTexture);//
                    ctx.cmd.SetComputeTextureParam(passData.ComputeShader_BlurPass,passData.kernel,ShaderID.ComputeShaderResult, passData.InpOutTexture );
                    ctx.cmd.SetComputeFloatParam(passData.ComputeShader_BlurPass,ShaderID.LightTextureBrightness,passData.LightData.LightTextureBrightness);
                    ctx.cmd.SetComputeFloatParam(passData.ComputeShader_BlurPass,ShaderID.LightTextureContrast,passData.LightData.LightTextureContrast);
                    ctx.cmd.DispatchCompute(passData.ComputeShader_BlurPass,passData.kernel,
                Mathf.CeilToInt( passData.width / (float) passData.ThreadX),
                Mathf.CeilToInt(  passData.height /(float) passData.ThreadY),
                        1);
                });
                if (final)
                builder.SetGlobalTextureAfterPass(_InputTexture,ShaderID.ShaderPassOutput);
            }
        }
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Clear the render target to black
            //context.cmd.ClearRenderTarget(true, true, Color.yellow);
            //context.cmd.DrawProcedural(Matrix4x4.identity, data.material, data.useMSAA ? 1 : 0, MeshTopology.Triangles, 3, 1, null);
            // Draw the objects in the list
            data.drawSettings.overrideMaterial = data.material;
            context.cmd.DrawRendererList(data.rendererListHandle);
        }

    }
}
#endif
