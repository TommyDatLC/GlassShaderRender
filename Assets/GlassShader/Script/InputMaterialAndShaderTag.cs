using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if GLASSSHADER_USING_URP
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace GlassShader.CPURenderPass
{
    [Serializable]
    public class InputMaterialAndShaderTag
    {
        public bool enable = true;
        public GlassMaterialContainer container;
#if GLASSSHADER_USING_HDRP
       public LayerMask layerMask;
#endif
#if GLASSSHADER_USING_URP
        public TextureHandle OutputPass,OutputPassNoMsaa;
        
#endif
#if GLASSSHADER_USING_BUILTIN
        public List<Graphic> GraphicsNeedToReRender = new List<Graphic>();
#endif
    }
}
