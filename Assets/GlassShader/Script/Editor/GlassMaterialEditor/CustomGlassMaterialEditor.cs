
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#if GLASSSHADER_USING_URP
using UnityEditor.Rendering.Universal;
#endif

namespace GlassShader.Script.Editor.GlassMaterialEditor
{
    
    [CustomEditor(typeof(Material))]
    public class CustomGlassMaterialEditor : MaterialEditor
    {
        private string DocsLink =
            "https://docs.google.com/document/u/1/d/1gAo9GlgroufWS__FInBhYHjALpYrkciPj3OQHgoSBwQ/edit?usp=sharing";
        // public override void OnInspectorGUI()
        // {
        //     
        //     
        //     else
        //     {
        //         base.DrawDefaultInspector();
        //         
        //         base.OnInspectorGUI();
        //     }
        // }
        public override VisualElement CreateInspectorGUI()
        {
            
            Material thisMaterial = (Material)target;

            if (thisMaterial.shader.name != "datdau/GlassShader")
            {
                var container = (new IMGUIContainer(() =>
                {
                    base.OnInspectorGUI();
                }));
                return container;
            }
            // Create a new VisualElement to be the root of our Inspector UI.
            VisualElement myInspector = new VisualElement();
            var visualUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/GlassShader/EditorUI/GlassMaterialController.uxml");
            var root = visualUXML.Instantiate();
            
            Button button_OpenDocs = root.Q<Button>("OpenDoc");
            Button button_OpenOldControl = root.Q<Button>("OpenOldMaterialControl");//
            Button button_OpenCurrentRender = root.Q<Button>("btn_openCurrentRender");
            button_OpenDocs.clicked += () =>
            {
                Application.OpenURL(DocsLink);
            };
            button_OpenOldControl.clicked += () =>
            {
                GlassMaterialOldEditorWindow.Open(thisMaterial);
            };
            
            button_OpenCurrentRender.clicked += () =>
            {
                RendererWindow renderWindow = EditorWindow.GetWindow<RendererWindow>();
                renderWindow.Show();
            };
            var settingRoot = root.Q<VisualElement>("Setting");
            // Execute to get the material properties and draw to the material panel
            AutoRenderMaterialProperties.Execute(thisMaterial,settingRoot);
            return root;
        }
        
        
    }
    
}

