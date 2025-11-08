using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Linq;
using GlassShader.CPURenderPass;
using GlassShader.Script.Editor;
using GlassShader.Script.Editor.CreateGlassAsset;
#if GLASSSHADER_USING_URP
using UnityEngine.Rendering.Universal;
using GlassShader;
#endif
using Object = UnityEngine.Object;

public class GlassShaderGenerator
{
    [MenuItem("Assets/Create/Glass Shader Asset", false, 0)]
    public static void CreateGlassShaderSetup()
    {
        
        string hash = GenerateShortHash();
        CreateGlassAssetWindow.Open(hash, OnCreateAssetWindowFinish);
    }

    private static void OnCreateAssetWindowFinish(string name)
    {
        string hash = name;
        string selectedPath = GetSelectedPathOrFallback();
        string folderPath = Path.Combine(selectedPath, $"{hash}_GlassShader");
        Directory.CreateDirectory(folderPath);

        // 1. Create shader
        string shaderPath = Path.Combine(folderPath, $"{hash}_GlassShaderFile.shader");
        string shaderContent = GenerateShaderWithLightTag(hash);
        File.WriteAllText(shaderPath, shaderContent);
        AssetDatabase.ImportAsset(shaderPath);
        Shader customShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);

        // 2. Create MarkMaterial
        Material markMat = new Material(customShader);
        AssetDatabase.CreateAsset(markMat, Path.Combine(folderPath,$"MarkMaterial_{hash}.mat"));
        
        // 3. Create GlassMaterial using datdau/GlassShader
        Shader glassShader = Shader.Find("datdau/GlassShader");
        if (glassShader == null)
        {
            Debug.LogError("Shader 'datdau/GlassShader' not found!");
            return;
        }

        Material glassMat = new Material(glassShader);
        AssetDatabase.CreateAsset(glassMat, Path.Combine(folderPath, $"GlassMaterial_{hash}.mat"));
        // Create new Container
        GlassMaterialContainer GlassMaterialContainer_obj = new GlassMaterialContainer();
        GlassMaterialContainer_obj.GlassMaterial = glassMat;
        GlassMaterialContainer_obj.hashID = hash;
        GlassMaterialContainer_obj.MarkMaterial = markMat;
        AssetDatabase.CreateAsset(GlassMaterialContainer_obj, Path.Combine(folderPath, $"{hash}.asset"));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 4. Add RenderFeature override
#if GLASSSHADER_USING_URP
        AddDrawObjectsRenderFeature(GlassMaterialContainer_obj);
#endif
#if GLASSSHADER_USING_HDRP
        SetupGlassRenderPassHDRP.SetupGlassPass(hash,glassMat);
#endif
        Debug.Log($"✅ Created GlassShader setup in: {folderPath}");
    }

    static string GetSelectedPathOrFallback()
    {
        string path = "Assets";
        foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }

            break;
        }

        return path;
    }

    static string GenerateShortHash()
    {
        string guid = System.Guid.NewGuid().ToString("N");
        return guid.Substring(0, 8); // shorten for readability
    }

    static string GenerateShaderWithLightTag(string tag)
    {
        return $@"
Shader ""Custom/GeneratedShader_{tag}""
{{
    SubShader
    {{
        Tags {{ ""RenderPipeline"" = ""UniversalPipeline"" ""RenderType""=""Transparent"" ""LightMode"" = ""{tag}"" }}
        Pass {{ }}
    }}
}}";
    }
//
    
#if GLASSSHADER_USING_URP
    static void AddDrawObjectsRenderFeature(GlassMaterialContainer container)
    {
        
        var urpDataGUIDs = AssetDatabase.FindAssets("t:UniversalRendererData");
        if (urpDataGUIDs.Length == 0)
        {
            Debug.LogWarning("No UniversalRendererData asset found in project.");
            return;
        }

        foreach (string urpDataGUID in urpDataGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(urpDataGUID);
            string path_ComputerShader_BlurPass =   "Assets/GlassShader/Script/HLSL/GlassShader_BlurPass.compute";
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            ComputeShader BlurPassShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(path_ComputerShader_BlurPass);
            if (rendererData == null)
            {
                Debug.LogError("UniversalRendererData could not be loaded.");
                return;
            }
// /
            DrawGlassShaderWithOverrideMaterial feature = null;
            var t = rendererData.rendererFeatures.FirstOrDefault(obj => obj is DrawGlassShaderWithOverrideMaterial);
            if (t != null)
                feature = t as DrawGlassShaderWithOverrideMaterial;
            else
            {
                    feature = ScriptableObject.CreateInstance<DrawGlassShaderWithOverrideMaterial>();
                    
                    AssetDatabase.AddObjectToAsset(feature,rendererData);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out var guid, out long localId);
                    rendererData.rendererFeatures.Add(feature);
                    Debug.Log($"Created DrawGlassShaderWithOverrideMaterial: {guid}, {localId}");
                    // Get the function ValidateRendererFeatures using hack
                    Type typeOfRenderdata = typeof(UniversalRendererData);
                    var validRenderFeatureMethodInfo =  typeOfRenderdata.GetMethod("ValidateRendererFeatures",System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    validRenderFeatureMethodInfo?.Invoke(rendererData,null);
                   
            }
            feature.ComputeShader_BlurPass = BlurPassShader;
            // Get in the list any null InputMaterialAndShaderTag
            var CheckForNullInput = feature.ListRenderObjects.FirstOrDefault(obj => obj.container == null);
          
            // it not found any
            if (CheckForNullInput == null)
            {
                // add new component
                var NewRenderObject = new InputMaterialAndShaderTag();
                NewRenderObject.container = container;
                // Add new feature to the ListRenderObject
                feature.ListRenderObjects.Add(
                    NewRenderObject
                );
            }
            else
            {
                // if found one
                CheckForNullInput.container = container;
            }

            
            EditorUtility.SetDirty(rendererData);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✅ Render Feature added or updated.");
    }

#endif

}

