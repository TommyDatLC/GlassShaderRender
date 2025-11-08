using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassShader.CPURenderPass
{
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    public class GlassMaterialAttach : MonoBehaviour
    {

        private Graphic _graphic;
        private Canvas _canvas;

        public GlassMaterialContainer glassMaterialContainer_instance;

        void Start()
        {
            
        }
        
        void FixedUpdate()
        {
            
        }
        private void OnValidate()
        {
            
            // nếu glass material khác null
            if (glassMaterialContainer_instance != null)
            {
                SetNewMaterial(glassMaterialContainer_instance);
            }
        }
        
        void SetNewMaterial(GlassMaterialContainer container)
        {
            if (!_graphic)
                _graphic = GetComponent<Graphic>();
            if (!_canvas)
                _canvas = GetComponentInParent<Canvas>();
            _graphic.material = container.MarkMaterial;
            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogError("[Simple glass] Cannot render the material as a Screen Space Overlay Canvas, Please change render mode of the canvas");
            }
            
        }
        
    }
    //
  
}