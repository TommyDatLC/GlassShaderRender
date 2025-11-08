using UnityEngine;

namespace GlassShader.CPURenderPass
{
    public class ShaderID
    {
       public static int GlobalTexture = Shader.PropertyToID("_GlobalTexture");
       public static int ShaderPassOutput = Shader.PropertyToID("_Pass0Output");
       public static int BlurIntensity = Shader.PropertyToID("_BlurIntensity");
       public static int EdgeFindingMethod = Shader.PropertyToID("_EdgeFindingMethod");
       public static int NumberOfPass = Shader.PropertyToID("_EdgeFindingPass");
       public static int RateCamvsCamBuffer = Shader.PropertyToID("_BlitSize");
       public static int CameraActualBuffer = Shader.PropertyToID("_CameraActualBuffer");
       
       public static int LightTexture = Shader.PropertyToID("_LightTexture");
       public static int LightTextureContrast = Shader.PropertyToID("_LightLayerContrast");
       public static int LightTextureBrightness = Shader.PropertyToID("_LightLayerBrightness");
       public static int LightTextureTilingAndoffset = Shader.PropertyToID("_LightTexture_ST");
       public static int ComputeShaderResult = Shader.PropertyToID("_Result");
       public static int InputComputeShaderBlurPass = Shader.PropertyToID("_Input");
       public static int Blur_ComputeShader_init = Shader.PropertyToID("_Init");
       
       public static int ComputeShader_CalcOutput_Input = Shader.PropertyToID("_BlitTexture");
       public static int ComputeShader_CalcOutput_BlitTexture = Shader.PropertyToID("_BlitTexture");
       public static int ComputeShader_CalcOutput_CameraActualTexture = Shader.PropertyToID("_CameraActualBuffer");
       
    }
}