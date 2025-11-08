Shader "datdau/GlassShader"
{
    Properties
    {
                 
      [HideInInspector]  _MainTex("MainTex", 2D) = "black" {}
//        [Header(Edge Setting)]
        //[KeywordEnum(Precise,Approximate (Faster))] _EdgeFindingMethod("Edge Finding Method",Integer) = 0
        //_EdgeFindingPass("Number of pass for edge finding",Integer) = 3
        _EdgeRadius("Edge Radius",Range(0,50)) = 0
        [Header(Distortion Setting)]
        [RangeInt(0,100)]  _BlurIntensity("Blur Intensity O(n)",Integer) = 10
        _DissolveRadius("Dissolve Radius",Range(1,100)) = 3
        _DissolveScaleX("Dissolve Scale X",Range(0.90,1.1   )) = 0.45
        _DissolveScaleY("Dissolve Scale Y",Range(-10,10)) = 0.5
        _DissolveDirectionStrength("Dissolve Direction Strength",Range(-0.3,0.3)) = 0.06
        
        [Header(Light Texture Setting)]
        _LightTexture("Light Texture", 2D) = "white" {}
        _LightLayerContrast("Light layer Contrast",Range(0,10)) = 1
        _LightLayerBrightness("Light layer Brightness",Range(-10,10)) = 0
        
        [Header(Color Dim Setting)]
        _DimColor("Tint Color",Color) = (0,0,0,0)
        _Color1Gradient("Color 1 ",Color) = (0,0,0,0)
        _stop1("Gradient Stop 1",Range(0,1)) = 0.6
        _Color2Gradient("Color 2 ",Color) = (0,0,0,0)
         _stop2("Gradient Stop 2",Range(0,1)) = 0.6
        _Color3Gradient("Color 3 ",Color) = (0,0,0,0)
        [KeywordEnum(Horiziontal,Vertical)] _DirectionOfGradient("Direction of Gradient",Integer) = 0
        [KeywordEnum(Screen Space,Texture Coordinate)] _SampleTechnique("Sampling Technique",Integer) = 0
   
       //_GlobalTexture("Global tex",2D )= "white" {}
        //_Pass0Output("Global tex",2D )= "white" {}
     //  _rateColorBuffer("rate",Vector)= (0,0,0,0)
      // _Xf("Xf",Range(0,1920)) = 0
       //_Yf("Yf",Range(0,1920)) = 0
    }
    
    SubShader
    {

        Cull Off
        
        ZWrite Off
        ZTest Off
        Tags { "RenderType"="Transparent" }
     
        Pass
        {
           
         name "pass0"
        HLSLINCLUDE
        float4 DbgOutput = 0;
        float4 a;
        #include  "UnityCG.cginc"
 
// /      //#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
        
        // #include "UnityShaderVariables.cginc"
        // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

        #if defined(UNITY_RENDER_PIPELINE_URP)
          #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
          #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            
      
        #else
        # define SAMPLER(x) SamplerState x
        #endif


          #include  "HLSL/EdgeFinding.hlsl"
          #include "HLSL/Dissolve.hlsl"
          #include "HLSL/GuassianBlur.hlsl"
        #include "HLSL/Gradien.hlsl"
     
        #pragma vertex vert
        #pragma fragment frag
          float _EdgeRadius;
       float4 _rateColorBuffer;
          struct Attribute
            {
                float4 PositionOS : POSITION;
         
                float2 uv : TEXCOORD0;
              
            };
            struct Vrayings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 screenUV : TEXCOORD1;
                
            };
        
            Vrayings vert(Attribute cord)
            {
                
                Vrayings OUT;
                #if defined(UNITY_RENDER_PIPELINE_URP)
                OUT.positionHCS =  TransformObjectToHClip(cord.PositionOS.xyz);
                #else
                OUT.positionHCS = UnityObjectToClipPos(cord.PositionOS.xyz) ;
                #endif
                OUT.uv = cord.uv;
                OUT.screenUV = OUT.positionHCS.xy  / OUT.positionHCS.w  *  0.5 + 0.5 ;
                // rate always smaller than one
                OUT.screenUV.y = 1-OUT.screenUV.y;
                // OUT.screenUV /= _rateColorBuffer.xy;
                return OUT;
            }
          
        ENDHLSL
           
            HLSLPROGRAM
            
             float _EdgeFindingMethod;
             float _EdgeFindingMethod_Percise = 0;
             float _EdgeFindingMethod_Approximate = 1;
           
            float _DissolveRadius;
            float _DissolveScaleX;
            float _DissolveScaleY;
             Texture2D  _MainTex;
            SamplerState sampler_MainTex;
             Texture2D _GlobalTexture;
             
            SAMPLER(sampler_GlobalTexture);
     
            float4 _DimColor;
             int _SampleTechnique;
             #define SAMPLE_TECHNIQUE_SCREEN_SPACE 0
             #define SAMPLE_TECHNIQUE_TEXCOORD 1
             int _DirectionOfGradient;
             #define DIRECTION_OF_GRADIENT_HOR 0
             #define DIRECTION_OF_GRADIENT_VER 1
             SamplerState sampler_UnityFBInput0;
           // SamplerState sampler_UnityFBInput0;

            float4 frag(Vrayings IN) : SV_Target
            {
               
                float4 maintext = _MainTex.Sample(sampler_MainTex,IN.uv);
              //  col = _UnityFBInput0.Load() (uint3(IN.screenUV * _ScreenSize,0));
            
                if (maintext.w == 0)
                    return 0;
                float4 col =  _GlobalTexture.Sample(sampler_GlobalTexture,IN.screenUV);
                
                float4 Dissolve = CheckCanDissolve(_GlobalTexture,sampler_GlobalTexture,_MainTex,sampler_MainTex,IN.uv,IN.screenUV,_DissolveRadius,_DissolveScaleX,_DissolveScaleY,col);
                col = Dissolve;
                float4 GradCol;
                float2 UVSample;
                // check for uv type user use
                if (_SampleTechnique == SAMPLE_TECHNIQUE_SCREEN_SPACE)
                    UVSample = IN.screenUV;
                else
                    UVSample = IN.uv;
                // check for direction user use
                if (_DirectionOfGradient == DIRECTION_OF_GRADIENT_HOR)
                     GradCol =  CreateGradient(UVSample.x);
                else
                     GradCol =  CreateGradient(UVSample.y);
                col = lerp(col,_DimColor,_DimColor.w);
                col = lerp(col,GradCol,GradCol.w);
                   if (DbgOutput.w > 0)
                    return DbgOutput;
                 return col; 
                
            }
            ENDHLSL
        }
        Pass
        {
          
          
  
            
            HLSLPROGRAM
            
          
            Texture2D _Pass0Output;
            SAMPLER(sampler_Pass0Output);
            Texture2D _MainTex;
            SAMPLER(sampler_MainTex);
            Texture2D _GlobalTexture;
            SAMPLER(sampler_GlobalTexture);
            Texture2D _LightTexture;
            SAMPLER(sampler_LightTexture);
            float _LightLayerContrast;
            float _LightLayerBrightness;
            
            float4 frag(Vrayings IN) : SV_Target0
            {
                
               float4 maintext = _MainTex.Sample(sampler_MainTex,IN.uv);
                  if (maintext.w <= 0)
                    return 0;
               // float4 col = GaussianBlur(_Pass0Output,sampler_Pass0Output,
               //     _LightTexture,sampler_LightTexture,_LightLayerContrast,_LightLayerBrightness,
               //     _GlobalTexture,sampler_GlobalTexture,IN.screenUV);
                return 0;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "PASS 2"
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest Off
            HLSLPROGRAM
            
            Texture2D  _MainTex;
            SamplerState sampler_MainTex;
            Texture2D  _Pass0Output;
            SamplerState sampler_Pass0Output;
            float4 frag(Vrayings IN) : SV_Target0
            {
                float4 maintext = _MainTex.Sample(sampler_MainTex,IN.uv);
                  if (maintext.w <= 0.01)
                    return 0;
                 float4  col = 0;
                 float edge = LaplacianEdge(_MainTex,sampler_MainTex,IN.uv,_EdgeRadius);
                 if (edge > 0)
                    col = edge;
                 else
                    col =  _Pass0Output.Sample(sampler_Pass0Output,IN.screenUV);
                return col;

                
            }
            ENDHLSL
        }
//        Pass
//        {
//            Name "CalDistanceFeildPass"
//                HLSLPROGRAM
//                Texture2D  _MainTex;
//                SamplerState sampler_MainTex;
//                float4 frag(Vrayings IN) : SV_Target0
//                {
//                    int w,h;
//                    float4 distance;
//                    _MainTex.GetDimensions(w,h);
//                    float2 textelSize = 1 / float2(w,h);
//                    [loop]
//                    for (int k = w; k  >= 1; k /= uint(2))
//                    {
//                       [loop]
//                        for (int  i = -k;i <= k ;i+= k)
//                            [loop]
//                            for (int  j = -k;j <= k ;j+= k)
//                            {
//                                float2 offset = (i * textelSize,j * textelSize);
//                                // neu maintex this pixel co color va pixel neighbor ko co color
//                                float neighBorAlpha = _MainTex.Sample(sampler_MainTex,IN.uv + offset ).w;
//                                float thisPixelAlpha = _MainTex.Sample(sampler_MainTex,IN.uv + offset ).w;
//                                if (neighBorAlpha == 0  &&  thisPixelAlpha != 0)
//                                {
//                                    // Set distance cua pixel nay
//                                    distance = length(offset);
//                                }
//                                //// neu maintex 
//                                if (neighBorAlpha != 0 && thisPixelAlpha != 0 )
//                                {
//                                    distance = min(thisPixelAlpha,neighBorAlpha + distance);
//                                }
//                            }
//                    }
//                    return distance;
//                }
//                ENDHLSL
//            
//        }
    }
}

