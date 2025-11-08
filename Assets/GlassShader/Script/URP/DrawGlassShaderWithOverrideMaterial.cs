#if GLASSSHADER_USING_URP
using System;
using System.Collections.Generic;
using GlassShader.CPURenderPass;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace GlassShader
{
   

    public class DrawGlassShaderWithOverrideMaterial : ScriptableRendererFeature
    {

        DrawObjectsPass drawObjectsPass;
        public LayerMask layerMask = ~0;
        public ComputeShader ComputeShader_BlurPass;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        public List<InputMaterialAndShaderTag> ListRenderObjects = new List<InputMaterialAndShaderTag>();
        public static DrawGlassShaderWithOverrideMaterial instance;
        public override void Create()
        {
                instance = this;
                // Create the render pass that draws the objects, and pass in the override material
                     drawObjectsPass = new DrawObjectsPass(ListRenderObjects, layerMask,ComputeShader_BlurPass);
                 //  blitCamTex = new BlitCameraTexturePass(renderPassEvent);
                // drawObjectsPass.LayerMask = layerMask;
                // drawObjectsPass.ListRenderObjects =  ListRenderObjects;
                // // Insert render passes after URP's post-processing render pass
                    drawObjectsPass.renderPassEvent = renderPassEvent;
                //
        }
 
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //   // Add the render pass to the URP rendering loop
         
              
            renderer.EnqueuePass(drawObjectsPass );
            
        }
    } 
    
}
#endif