// File: Assets/Editor/SetupGlassRenderPassHDRP_Flexible.cs
#if GLASSSHADER_USING_HDRP
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace GlassShader.Script.Editor
{
    public static class SetupGlassRenderPassHDRP
    {

        public static void SetupGlassPass(string hashID,Material material1)
        {
            if (material1 == null)
            {
                Debug.LogError("[SetupGlassPass] material1 == null. Please provide a valid Material.");
                return;
            }

            // 1) Find a CustomPassVolume in the current scene (include inactive)
            CustomPassVolume volume = UnityEngine.Object.FindObjectsOfType<CustomPassVolume>(true).FirstOrDefault();
            GameObject targetGO = null;
            if (volume == null)
            {
                targetGO = new GameObject("Glass_CustomPassVolume");
                Undo.RegisterCreatedObjectUndo(targetGO, "Create Glass CustomPassVolume GO");
                volume = targetGO.AddComponent<CustomPassVolume>();
                Undo.RegisterCreatedObjectUndo(volume, "Add CustomPassVolume");
                try { volume.injectionPoint = CustomPassInjectionPoint.BeforeTransparent; } catch { }
                Debug.Log("[SetupGlassPass] Created new GameObject and CustomPassVolume.");
                // /
            }
            else
            {
                targetGO = volume.gameObject;
                Debug.Log("[SetupGlassPass] Found existing CustomPassVolume: " + targetGO.name);
            }

            // 2) Find the GlassRenderPass type in loaded assemblies
            Type glassType = FindTypeInAppDomain(" GlassShader.HDRP.GlassRenderPass");
            if (glassType == null)
            {
                Debug.LogError("[SetupGlassPass] Could not find type 'GlassRenderPass' in the project.");
                return;
            }

            // 3) Try to find an existing GlassRenderPass instance in volume.customPasses
            //    Use SerializedProperty if the customPasses array stores UnityEngine.Object, otherwise try reflection.
            UnityEngine.Object foundUnityPass = null;
            object foundPlainPass = null;

            SerializedObject volSO = new SerializedObject(volume);
            SerializedProperty customPassesProp = volSO.FindProperty("customPasses");

            // Try serialized path first (works when customPasses is an array of UnityEngine.Object)
            if (customPassesProp != null && customPassesProp.isArray)
            {
                for (int i = 0; i < customPassesProp.arraySize; ++i)
                {
                    var elem = customPassesProp.GetArrayElementAtIndex(i);
                    if (elem != null && elem.objectReferenceValue != null && elem.objectReferenceValue.GetType() == glassType)
                    {
                        foundUnityPass = elem.objectReferenceValue;
                        break;
                    }
                }
            }

            // If not found, fallback to reflection on the "customPasses" field (could be List<T> of plain objects)
            if (foundUnityPass == null)
            {
                FieldInfo cpField = typeof(CustomPassVolume).GetField("customPasses", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (cpField != null)
                {
                    var listObj = cpField.GetValue(volume);
                    if (listObj != null)
                    {
                        // iterate the enumerable
                        foreach (var item in (IEnumerable)listObj)
                        {
                            if (item != null && item.GetType() == glassType)
                            {
                                // If item is a UnityEngine.Object subclass, store as such; otherwise store as plain object
                                if (item is UnityEngine.Object uo) foundUnityPass = uo;
                                else foundPlainPass = item;
                                break;
                            }
                        }
                    }
                }
            }

            object passInstance = null;
            bool passIsUnityObject = false;

            // 4) If no instance found, create one
            if (foundUnityPass == null && foundPlainPass == null)
            {
                // If the type derives from UnityEngine.Object, create a ScriptableObject instance
                if (typeof(UnityEngine.Object).IsAssignableFrom(glassType))
                {
                    passInstance = ScriptableObject.CreateInstance(glassType);
                    passIsUnityObject = true;
                    // name it
                    (passInstance as UnityEngine.Object).name = "GlassRenderPass_Instance";
                    Undo.RegisterCreatedObjectUndo(passInstance as UnityEngine.Object, "Create GlassRenderPass Instance");
                }
                else
                {
                    // plain .NET class
                    passInstance = Activator.CreateInstance(glassType);
                    passIsUnityObject = false;
                }

                // Add the new instance into customPasses
                bool added = false;
                if (customPassesProp != null && customPassesProp.isArray && passIsUnityObject)
                {
                    int idx = customPassesProp.arraySize;
                    customPassesProp.InsertArrayElementAtIndex(idx);
                    var newElem = customPassesProp.GetArrayElementAtIndex(idx);
                    newElem.objectReferenceValue = passInstance as UnityEngine.Object;
                    volSO.ApplyModifiedProperties();
                    added = true;
                }
                else
                {
                    FieldInfo cpField = typeof(CustomPassVolume).GetField("customPasses", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (cpField != null)
                    {
                        var listObj = cpField.GetValue(volume);
                        if (listObj != null)
                        {
                            MethodInfo addMethod = listObj.GetType().GetMethod("Add");
                            if (addMethod != null)
                            {
                                addMethod.Invoke(listObj, new object[] { passInstance });
                                added = true;
                            }
                        }
                    }
                }

                if (!added)
                    Debug.LogWarning("[SetupGlassPass] Could not add GlassRenderPass to customPasses (structure not as expected). Check CustomPassVolume.customPasses.");
            }
            else
            {
                if (foundUnityPass != null)
                {
                    passInstance = foundUnityPass;
                    passIsUnityObject = true;
                    Debug.Log("[SetupGlassPass] Using existing GlassRenderPass instance (UnityEngine.Object).");
                }
                else
                {
                    passInstance = foundPlainPass;
                    passIsUnityObject = false;
                    Debug.Log("[SetupGlassPass] Using existing GlassRenderPass instance (plain class).");
                }
            }

            // 5) Load ComputeShaders and assign them via reflection or SerializedObject
            string blurPath = "Assets/GlassShder/Script/HLSL/GlassShader_BlurPass.compute";
            string calcPath = "Assets/GlassShder/Script/HLSL/CaculateNewOutputImage.compute";
            // try some common typo variants
            string[] blurCandidates = new string[] {
                "Assets/GlassShader/Script/HLSL/GlassShader_BlurPass.compute"
            };
            string[] calcCandidates = new string[] {
                "Assets/GlassShader/Script/HLSL/GlassShader_CaculateNewOutputImage.compute"
            };

            ComputeShader csBlur = null;
            ComputeShader csCalc = null;
            foreach (var p in blurCandidates)
            {
                var r = AssetDatabase.LoadAssetAtPath<ComputeShader>(p);
                if (r != null) { csBlur = r; break; }
            }
            foreach (var p in calcCandidates)
            {
                var r = AssetDatabase.LoadAssetAtPath<ComputeShader>(p);
                if (r != null) { csCalc = r; break; }
            }
            if (csBlur == null) Debug.LogWarning("[SetupGlassPass] BlurPass.compute not found in any tested paths.");
            if (csCalc == null) Debug.LogWarning("[SetupGlassPass] CaculateNewOutputImage.compute not found in any tested paths.");

            // Try set via SerializedObject if pass is a UnityEngine.Object
            if (passIsUnityObject)
            {
                var passUnity = passInstance as UnityEngine.Object;
                SerializedObject passSO = new SerializedObject(passUnity);
                bool appliedBlur = false, appliedCalc = false;

                var propBlur = passSO.FindProperty("ComputeShader_ComputeBlurPass");
                if (propBlur != null) { propBlur.objectReferenceValue = csBlur; appliedBlur = true; }
                var propCalc = passSO.FindProperty("ComputeShader_CaculateOutputImage");
                if (propCalc != null) { propCalc.objectReferenceValue = csCalc; appliedCalc = true; }

                if (!appliedBlur || !appliedCalc)
                {
                    // fallback reflection
                    FieldInfo fBlur = passInstance.GetType().GetField("ComputeShader_ComputeBlurPass", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo fCalc = passInstance.GetType().GetField("ComputeShader_CaculateOutputImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (!appliedBlur && fBlur != null) fBlur.SetValue(passInstance, csBlur);
                    if (!appliedCalc && fCalc != null) fCalc.SetValue(passInstance, csCalc);
                }

                passSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(passUnity);
            }
            else
            {
                // plain object -> reflection set fields
                FieldInfo fBlur = passInstance.GetType().GetField("ComputeShader_ComputeBlurPass", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo fCalc = passInstance.GetType().GetField("ComputeShader_CaculateOutputImage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fBlur != null) fBlur.SetValue(passInstance, csBlur);
                else Debug.LogWarning("[SetupGlassPass] Field ComputeShader_ComputeBlurPass not found on GlassRenderPass (plain).");
                if (fCalc != null) fCalc.SetValue(passInstance, csCalc);
                else Debug.LogWarning("[SetupGlassPass] Field ComputeShader_CaculateOutputImage not found on GlassRenderPass (plain).");
            }

            // 6) Update the render object list (InputMaterialAndShaderTag)
            // If pass is a UnityEngine.Object => use SerializedObject to edit the array
            bool successAssign = false;
            if (passIsUnityObject)
            {
                var passUnity = passInstance as UnityEngine.Object;
                SerializedObject passSO = new SerializedObject(passUnity);
                SerializedProperty arrayProp = FindArrayPropertyContainingMaterial(passSO);

                if (arrayProp == null)
                {
                    Debug.LogWarning("[SetupGlassPass] Could not find serialized render object array in GlassRenderPass.");
                }
                else
                {
                    passSO.Update();
                    // find an element where Material == null
                    for (int i = 0; i < arrayProp.arraySize; ++i)
                    {
                        var elem = arrayProp.GetArrayElementAtIndex(i);
                        if (elem == null) continue;
                        var matProp = elem.FindPropertyRelative("Material");
                        if (matProp != null && matProp.objectReferenceValue == null)
                        {
                            Undo.RecordObject(passUnity, "Assign material to render object");
                            matProp.objectReferenceValue = material1;
                            var shaderTagProp = elem.FindPropertyRelative("ShaderTag");
                            if (shaderTagProp != null) shaderTagProp.stringValue = hashID;
                            successAssign = true;
                            break;
                        }
                    }
                    if (!successAssign)
                    {
                        // insert a new element
                        int newIndex = arrayProp.arraySize;
                        arrayProp.InsertArrayElementAtIndex(newIndex);
                        var newElem = arrayProp.GetArrayElementAtIndex(newIndex);
                        var matProp = newElem.FindPropertyRelative("Material");
                        if (matProp != null)
                        {
                            Undo.RecordObject(passUnity, "Add render object and assign material");
                            matProp.objectReferenceValue = material1;
                        }
                        var shaderTagProp = newElem.FindPropertyRelative("ShaderTag");
                        if (shaderTagProp != null) shaderTagProp.stringValue = hashID;
                        successAssign = true;
                    }
                    passSO.ApplyModifiedProperties();
                    EditorUtility.SetDirty(passUnity);
                }
            }
            else
            {
                // plain object: find a field List<T> or T[] where T has a 'Material' field
                FieldInfo targetField = null;
                Type elementType = null;
                foreach (var f in passInstance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    Type t = f.FieldType;
                    if (t.IsArray)
                    {
                        Type elem = t.GetElementType();
                        if (elem != null && HasMaterialField(elem)) { targetField = f; elementType = elem; break; }
                    }
                    else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        Type elem = t.GetGenericArguments()[0];
                        if (elem != null && HasMaterialField(elem)) { targetField = f; elementType = elem; break; }
                    }
                }

                if (targetField == null)
                {
                    Debug.LogWarning("[SetupGlassPass] Could not find a field for the render object list in GlassRenderPass (plain).");
                }
                else
                {
                    var listObj = targetField.GetValue(passInstance);
                    if (listObj == null)
                    {
                        // if it's a List<T>, create a new instance
                        if (targetField.FieldType.IsGenericType && targetField.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            var listType = typeof(List<>).MakeGenericType(elementType);
                            listObj = Activator.CreateInstance(listType);
                            targetField.SetValue(passInstance, listObj);
                        }
                        else if (targetField.FieldType.IsArray)
                        {
                            // create an empty array
                            Array arr = Array.CreateInstance(elementType, 0);
                            targetField.SetValue(passInstance, arr);
                            listObj = arr;
                        }
                    }

                    // handle List<T>
                    if (listObj is IList il)
                    {
                        // find an element with Material == null
                        for (int i = 0; i < il.Count; ++i)
                        {
                            var item = il[i];
                            var matF = item.GetType().GetField("Material", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (matF != null)
                            {
                                var matVal = matF.GetValue(item) as Material;
                                if (matVal == null)
                                {
                                    matF.SetValue(item, material1);
                                    var shaderTagF = item.GetType().GetField("ShaderTag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (shaderTagF != null) shaderTagF.SetValue(item, hashID);
                                    successAssign = true;
                                    break;
                                }
                            }
                        }

                        if (!successAssign)
                        {
                            // add a new element
                            var newElem = Activator.CreateInstance(elementType);
                            var matF = elementType.GetField("Material", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (matF != null) matF.SetValue(newElem, material1);
                            var shaderTagF = elementType.GetField("ShaderTag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (shaderTagF != null) shaderTagF.SetValue(newElem, hashID);
                            il.Add(newElem);
                            successAssign = true;
                        }
                    }
                    else if (listObj is Array arr)
                    {
                        // arrays are fixed length -> create a new array with one extra element
                        int oldLen = arr.Length;
                        Array newArr = Array.CreateInstance(elementType, oldLen + 1);
                        Array.Copy(arr, newArr, oldLen);
                        var newElem = Activator.CreateInstance(elementType);
                        var matF = elementType.GetField("Material", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (matF != null) matF.SetValue(newElem, material1);
                        var shaderTagF = elementType.GetField("ShaderTag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (shaderTagF != null) shaderTagF.SetValue(newElem, hashID);
                        newArr.SetValue(newElem, oldLen);
                        targetField.SetValue(passInstance, newArr);
                        successAssign = true;
                    }
                    else
                    {
                        Debug.LogWarning("[SetupGlassPass] renderObjects field is not an IList or Array (cannot automatically handle).");
                    }
                }
            }

            if (!successAssign) Debug.LogWarning("[SetupGlassPass] Could not automatically assign material/ShaderTag to render object.");

            // 7) Save/mark dirty and select the GameObject
            if (passIsUnityObject)
            {
                EditorUtility.SetDirty(passInstance as UnityEngine.Object);
            }
            EditorUtility.SetDirty(volume);
            EditorUtility.SetDirty(targetGO);
            if (targetGO.scene.IsValid()) EditorSceneManager.MarkSceneDirty(targetGO.scene);
            Selection.activeGameObject = targetGO;
            Debug.Log("[SetupGlassPass] Finished configuring GlassRenderPass on: " + targetGO.name);
        }

        // Helpers -------------------------------------------------------

        private static Type FindTypeInAppDomain(string typeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(typeName, false, true);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        private static bool HasMaterialField(Type t)
        {
            return t.GetField("Material", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
        }

        // iterate SerializedObject properties to find an array whose element has a 'Material' field
        private static SerializedProperty FindArrayPropertyContainingMaterial(SerializedObject so)
        {
            var prop = so.GetIterator();
            bool enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.isArray && prop.propertyType == SerializedPropertyType.Generic)
                {
                    if (prop.arraySize > 0)
                    {
                        var elem = prop.GetArrayElementAtIndex(0);
                        if (elem != null)
                        {
                            var mat = elem.FindPropertyRelative("Material");
                            if (mat != null)
                                return prop;
                        }
                    }
                    else
                    {
                        // try inserting a temporary element (wrapped in try/catch) to inspect structure
                        try
                        {
                            int idx = prop.arraySize;
                            prop.InsertArrayElementAtIndex(idx);
                            var inserted = prop.GetArrayElementAtIndex(idx);
                            if (inserted != null)
                            {
                                var mat = inserted.FindPropertyRelative("Material");
                                prop.DeleteArrayElementAtIndex(idx);
                                if (mat != null) return prop;
                            }
                        }
                        catch { }
                    }
                }
            }
            return null;
        }
    }
}
#endif
