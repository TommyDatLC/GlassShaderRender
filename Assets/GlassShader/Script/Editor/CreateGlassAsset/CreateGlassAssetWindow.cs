using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GlassShader.Script.Editor.CreateGlassAsset
{
    public class CreateGlassAssetWindow : EditorWindow
    {
        
        public static void Open(string defaultValue,Action<string> callback)
        {
            CreateGlassAssetWindow window = EditorWindow.GetWindow<CreateGlassAssetWindow>();
            window.Show();
            window.callback = callback;
            window._textField_name.textEdition.placeholder = defaultValue;
            window.defaultOutput = defaultValue;
        }
        Action<string> callback;
        private string defaultOutput;
        // [MenuItem("Window/My Custom UI Toolkit Window")]
        // public static void ShowWindow()
        // {
        //     CreateGlassAssetWindow window = EditorWindow.GetWindow<CreateGlassAssetWindow>();
        //     window.Show();
        //     TextField t =  window.rootVisualElement.Q<TextField>("textfeild_glassAssetName");
        // }    
        TextField _textField_name ;
        Button _button_create;
        private VisualTreeAsset _windowContentUXML;
        private void OnEnable()
        {
            _windowContentUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/GlassShader/EditorUI/CreateGlassAssetWindow.uxml");
            rootVisualElement.Add(_windowContentUXML.Instantiate());
            _textField_name = rootVisualElement.Q<TextField>("textfeild_glassAssetName");
            _button_create = rootVisualElement.Q<Button>("button_create");
            maxSize = new Vector2(400, 100);
            minSize = new Vector2(400, 100);
            _button_create.clicked += OnComplete;
            _textField_name.RegisterCallback<KeyDownEvent>((e) =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    OnComplete();
                }
            });
            titleContent.text = "Create glass asset panel";
        }

        void OnComplete()
        {
            var OutputText = _textField_name.text;
            foreach (char c in OutputText)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    EditorUtility.DisplayDialog("Error", "Please enter the name with no special or space character", "Ok");
                    return;
                    
                }
            }

            if (OutputText.Length == 0)
                OutputText =  defaultOutput;
            Close();
            callback.Invoke(OutputText);
        }
        private void OnGUI()
        {
            
            Event e = Event.current;
            if (e.type == EventType.KeyDown && 
                (e.keyCode == KeyCode.Return ||  e.keyCode == KeyCode.KeypadEnter))
            {
                OnComplete();
            }
            
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                Close();
            }
            
        }
    }
}