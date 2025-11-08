using UnityEditor;
using UnityEngine;

namespace GlassShader.Script.Editor.GlassMaterialEditor
{
    public class GlassMaterialOldEditorWindow : EditorWindow
    {
        private Material targetMaterial;
        private MaterialEditor materialEditor;
    
        public static void Open(Material material)
        {
            if (material == null) return;
            GlassMaterialOldEditorWindow window = GetWindow<GlassMaterialOldEditorWindow>("Glass Material Editor");
            window.targetMaterial = material;
            window.Init();
            window.Show();
        }

        private void Init()
        {
            if (targetMaterial != null)
            {
                materialEditor = (MaterialEditor)UnityEditor.Editor.CreateEditor(targetMaterial, typeof(MaterialEditor));
            }
        }

        private void OnGUI()
        {
            if (targetMaterial == null)
            {
            
                EditorGUILayout.HelpBox("No material selected.", MessageType.Warning);
                return;
            }

            if (materialEditor == null)
            {
                Init();
            }

            if (materialEditor != null)
            {
                materialEditor.DrawHeader();
        
                materialEditor.OnInspectorGUI();
            }
        }

        private void OnDisable()
        {
            if (materialEditor != null)
            {
                DestroyImmediate(materialEditor);
            }
        }
    }
}