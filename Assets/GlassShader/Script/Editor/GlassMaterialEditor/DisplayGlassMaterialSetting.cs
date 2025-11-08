// File: GlassMaterialContainerEditor.cs

using System;
using GlassShader.CPURenderPass;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GlassShader.Script.Editor.GlassMaterialEditor
{
    [CustomEditor(typeof(GlassMaterialAttach))]
    public class GlassMaterialContainerEditor : UnityEditor.Editor
    {
        CustomGlassMaterialEditor customGlassMaterialEditor;
        private void OnEnable()
        {
       
        
        }

       

        bool ShowMatEditor = false;
        public override VisualElement CreateInspectorGUI()
        {
            
            VisualElement container = new VisualElement();
            GlassMaterialAttach glassAttachComponent = (GlassMaterialAttach)target;
            // Create default view of the Attacher
            container.Add(new IMGUIContainer(() =>
            {
                base.OnInspectorGUI();
            }));

            Material materialNeedToShow = glassAttachComponent.glassMaterialContainer_instance?.GlassMaterial;
            // Create a new button
            Foldout f =  new Foldout();
    
            // set label text and change some paramater
            f.text = $"{glassAttachComponent.glassMaterialContainer_instance?.name} Editor";
            f.style.fontSize = 15;
            f.style.unityTextAlign = TextAnchor.MiddleCenter;
            f.style.unityFontStyleAndWeight = FontStyle.Bold;
            // add the material editor into the panel
            if (glassAttachComponent.glassMaterialContainer_instance)
            {
                var MaterialEditor = (CustomGlassMaterialEditor)CreateEditor(materialNeedToShow);
                f.Add(MaterialEditor.CreateInspectorGUI());
                container.Add(f);
                //
            }

            return container;
        }

        // public override void OnInspectorGUI()
        // {
        //     // Draw Inspector
        //     base.OnInspectorGUI();
        //
        //     
    
        // }
    }
}

// File: .cs

