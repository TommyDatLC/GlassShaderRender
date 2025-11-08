#if GLASSSHADER_USING_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace GlassShader.CPURenderPass
{
    public enum ColorToParse
    {
        OpaqueTexture,
        ScreenBuffer
    }
    public class BlitCameraTexturePass : ScriptableRenderPass
    {
        class PassData
        {
            public TextureHandle SceneColor;
        }
        //
        // public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        // {
        //     var camData = frameData.Get<UniversalCameraData>();
        //     var resourceData = frameData.Get<UniversalResourceData>();
        //     var desc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        //     desc.clearBuffer = false;
        //     desc.depthBufferBits = 0;
        //     TextureHandle hanle = renderGraph.CreateTexture(desc);
        //     CopyColorAndOutGlobalTex(renderGraph,hanle,resourceData,ShaderID.GlobalTexture);
        // }

        public static void CopyColorAndOutGlobalTex(RenderGraph renderGraph,TextureHandle outputTex,  
            UniversalResourceData resourceData,
            int id )
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Copy Color", out var passData))
            {
                builder.SetRenderAttachment(outputTex, 0);
               // builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read); // depth only for sampling if needed
                
                // Ensure we can read from the active camera texture
                builder.UseTexture(resourceData.cameraColor);
                passData.SceneColor = resourceData.activeColorTexture;
                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    
                    Blitter.BlitTexture(ctx.cmd,
                        data.SceneColor, // src
                        new Vector4(1, 1, 0, 0),          // scale/bias
                        0,                               // mip
                        false);           
                    // bilinear
                });
                
                // Make the copy available globally
                builder.SetGlobalTextureAfterPass(outputTex,  id);
            }
        }

        public static void Blit(RenderGraph renderGraph,TextureHandle inputTex,TextureHandle outputTex,  
           string name)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(name, out var passData))
            {
                builder.SetRenderAttachment(outputTex, 0);
                builder.UseTexture(inputTex);
                passData.SceneColor = inputTex;
                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    
                    Blitter.BlitTexture(ctx.cmd,
                        data.SceneColor, // src
                        new Vector4(1, 1, 0, 0),          // scale/bias
                        0,                               // mip
                        false);           
                    // bilinear
                });
                
           
                
            }
        }
    }
}
#endif