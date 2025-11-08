using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEngine;
#if GLASSSHADER_USING_HDRP
using GlassShader.HDRP;
using UnityEngine.Rendering.HighDefinition;
#endif
#if GLASSSHADER_USING_URP
using UnityEngine.Rendering.Universal;
#endif
namespace GlassShader.Script.Editor
{
    [InitializeOnLoad]
    public static class ReloadDetector
    {
        static ReloadDetector()
        {//
#if GLASSSHADER_USING_URP
            
           var RenderingDataGUID =  AssetDatabase.FindAssets("t:UniversalRendererData");
         
           foreach (string guid in RenderingDataGUID)
           {
               string path = AssetDatabase.GUIDToAssetPath(guid);
               
               UniversalRendererData guidToObject = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
               
               var glassRenderFeature_base =  guidToObject.rendererFeatures.FirstOrDefault(t => t is DrawGlassShaderWithOverrideMaterial);
               if (glassRenderFeature_base == null)
               {
                   Debug.LogError("[Simple Glass] Cannot find glassRenderFeature");
                   return;
               }
               DrawGlassShaderWithOverrideMaterial glassRenderFeature = glassRenderFeature_base as DrawGlassShaderWithOverrideMaterial;
               var listRenderObject = glassRenderFeature.ListRenderObjects;
               listRenderObject.RemoveAll(t => t.container == null);

           }
#endif
#if GLASSSHADER_USING_HDRP
           var CustomPass = GameObject.FindObjectOfType<CustomPassVolume>();
           if (CustomPass == null)
           {
               return;
           }
           var GlassRenderPass_base = CustomPass.customPasses.Find(t => t is GlassRenderPass);
           if (GlassRenderPass_base == null)
               return;
           var GlassRenderPass = GlassRenderPass_base as GlassRenderPass;
           GlassRenderPass.RenderObjects.RemoveAll(t => t.container.GlassMaterial == null);
#endif
            Debug.Log("👉[Simple glass] Clean glass render object with no material inside");
        }
    }
}
