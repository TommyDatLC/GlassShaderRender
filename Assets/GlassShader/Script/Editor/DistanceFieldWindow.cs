// using UnityEngine;
// using UnityEditor;
// using System.IO;
//
// public class JfaDistanceFieldGenerator : EditorWindow
// {
//     Texture2D inputTexture;
//     string savePath = "Assets/DistanceField_JFA.png";
//     ComputeShader jfaCompute;
//
//     [MenuItem("Tools/JFA Distance Field Generator")]
//     static void Init()
//     {
//         GetWindow<JfaDistanceFieldGenerator>("JFA Distance Field");
//     }
//     void OnGUI()
//     {
//         inputTexture = (Texture2D)EditorGUILayout.ObjectField("Input Texture", inputTexture, typeof(Texture2D), false);
//         jfaCompute = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/GlassShader/Script/Editor/JumpFlood.compute");// (ComputeShader)EditorGUILayout.ObjectField("Compute Shader", jfaCompute, typeof(ComputeShader), false);
//
//         EditorGUILayout.BeginHorizontal();
//         savePath = EditorGUILayout.TextField("Save Path", savePath);
//         if (GUILayout.Button("Browse", GUILayout.Width(70)))
//         {
//             string path = EditorUtility.SaveFilePanel("Save Texture", "Assets", "DistanceField.png", "png");
//             if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
//                 savePath = "Assets" + path.Substring(Application.dataPath.Length);
//         }
//         EditorGUILayout.EndHorizontal();
//
//         GUILayout.Space(20);
//         if (GUILayout.Button("Generate Distance Field (JFA)", GUILayout.Height(30)))
//         {
//             if (inputTexture == null || jfaCompute == null)
//             {
//                 EditorUtility.DisplayDialog("Error", "Please assign both Texture and Compute Shader", "OK");
//                 return;
//             }
//
//             RunJFA();
//         }
//     }
//
//     void RunJFA()
//     {
//         int size = inputTexture.width;
//
//         RenderTexture seedTex = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat);
//         seedTex.enableRandomWrite = true;
//         seedTex.Create();
//
//         RenderTexture resultTex = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat);
//         resultTex.enableRandomWrite = true;
//         resultTex.Create();
//
//         jfaCompute.SetInt("_TexSize", size);
//         jfaCompute.SetTexture(0, "_InputTex", inputTexture);
//         jfaCompute.SetTexture(0, "_SeedTex", seedTex);
//         jfaCompute.Dispatch(0, size / 8, size / 8, 1); // SeedInit
//
//         int jump = size / 2;
//         int ping = 0;
//
//         while (jump > 0)
//         {
//             jfaCompute.SetInt("_JumpSize", jump);
//             jfaCompute.SetTexture(1, "_SeedTex", ping == 0 ? seedTex : resultTex);
//             jfaCompute.SetTexture(1, "_ResultTex", ping == 0 ? resultTex : seedTex);
//             jfaCompute.Dispatch(1, size / 8, size / 8, 1);
//             ping = 1 - ping;
//             jump /= 2;
//         }
//
//         // Render final distance
//         jfaCompute.SetInt("_JumpSize", 1);
//         jfaCompute.SetTexture(2, "_SeedTex", ping == 0 ? seedTex : resultTex);
//         jfaCompute.SetTexture(2, "_ResultTex", resultTex);
//         jfaCompute.Dispatch(2, size / 8, size / 8, 1);
//
//         // Convert resultTex to Texture2D and save
//         Texture2D texOut = new Texture2D(size, size, TextureFormat.RGBA32, false);
//         RenderTexture.active = resultTex;
//         texOut.ReadPixels(new Rect(0, 0, size, size), 0, 0);
//         texOut.Apply();
//
//         byte[] png = texOut.EncodeToPNG();
//         File.WriteAllBytes(savePath, png);
//         AssetDatabase.ImportAsset(savePath);
//         Debug.Log("Saved Distance Field to: " + savePath);
//
//         seedTex.Release();
//         resultTex.Release();
//     }
// }
