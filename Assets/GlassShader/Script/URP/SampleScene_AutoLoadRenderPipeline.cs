using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class SampleScene_AutoLoadRenderPipeline : MonoBehaviour
{
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created 
    public RenderPipelineAsset rp;
    private void OnValidate()
    {
      
        QualitySettings.renderPipeline = rp;
        // var UniversalRPAsset = rp as UniversalRenderPipelineAsset;
        // UniversalRPAsset.rendererDataList.
        Debug.Log("Set the render pipeline to Assets/GlassShader/Settings/PC_RPAsset.asset");
    }//
    //
 

}
