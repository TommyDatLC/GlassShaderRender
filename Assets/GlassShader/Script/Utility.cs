using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GlassShader.CPURenderPass
{
    public class Utility
    {
        public static Vector4 GetMaterialTilingAndOffset(Material material,int PropID)
        {
            Vector2 Scale = material.GetTextureScale(PropID);
            Vector2 Offset = material.GetTextureOffset(PropID);

            Vector4 result =  new Vector4(Scale.x, Scale.y, Offset.x, Offset.y);
            return result;
        }

        public static Vector4 GetLightTextureScale(Material material)
        {
            return GetMaterialTilingAndOffset(material,ShaderID.LightTexture);
        }

        
    }
}