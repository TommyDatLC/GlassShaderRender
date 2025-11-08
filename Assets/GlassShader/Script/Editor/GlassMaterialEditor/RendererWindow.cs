using System;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;
#if GLASSSHADER_USING_URP
using UnityEngine.Rendering.Universal;
using GlassShader.CPURenderPass;
#endif
#if GLASSSHADER_USING_HDRP
using GlassShader.HDRP;
#endif

namespace GlassShader.Script.Editor.GlassMaterialEditor
{
    public class RendererWindow : EditorWindow
    {
        private Action GUICommand;
        private void OnEnable()
        {
            try
            {
#if GLASSSHADER_USING_URP
                var Asset = QualitySettings.GetRenderPipelineAssetAt(0);
                UniversalRenderPipelineAsset URP = Asset as UniversalRenderPipelineAsset;
                UniversalRendererData renderData = URP.rendererDataList[0] as UniversalRendererData;
                var GlassShaderRenderer = renderData.rendererFeatures.First((o) => o is DrawGlassShaderWithOverrideMaterial) as DrawGlassShaderWithOverrideMaterial;
                Debug.Log( GlassShaderRenderer);
                UnityEditor.Editor URPRenderAssetEditor = UnityEditor.Editor.CreateEditor(GlassShaderRenderer);
                            // Open 0 
        
#endif
#if GLASSSHADER_USING_HDRP
                UnityEditor.Editor URPRenderAssetEditor = UnityEditor.Editor.CreateEditor(FindAnyObjectByType<CustomPassVolume>());
#endif
                if (GUICommand == null)
                    GUICommand += () =>
                    {
                        URPRenderAssetEditor.OnInspectorGUI();                
                    };
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", "Cannot find the glass shader render", "Ok");
                Debug.LogException(e);
                throw;
            }
            titleContent.text = "Current glass renderer window";
        }
        Vector2 scroll;
        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUICommand.Invoke();
            EditorGUILayout.EndScrollView();
        }
    }
}