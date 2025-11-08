
static float2 MainTexDim;
float _DissolveDirectionStrength;
float4 Dissolve_Simple_Sin(Texture2D CameraTexutre, SamplerState sampler_CameraTexture,float2 ScreenUV,float2 UV,float ScaleX,float ScaleY,float dis)
{
    //
    //float2 center = (0.5, 0.5);
    float distance = dis ;
   // float dist = length(dir);
    
    // // góc hiện tại
    // float angle = atan2(dir.y, dir.x);
    //
    // // thêm dao động zigzag dựa trên khoảng cách
    // angle += sin(dist * ScaleX * 6.123) * ScaleY;
        
    // tọa độ mới
    // if (UV.x > 0.5)
    //     ScaleY = -ScaleY;
    float2 power = UV - float2(0.5,0.5);
 
    float2 newDir =   (ScreenUV - 0.5)  * (ScaleX  + (1 - distance) * (ScaleY  * _DissolveDirectionStrength))  + 0.5  ;

    return  CameraTexutre.Sample(sampler_CameraTexture,newDir);
}

bool DistanceCheck(Texture2D MainTex,SamplerState sampler_MainTex,float2 UV,float2 dir)
{
    float w,h;
    MainTex.GetDimensions(w,h);
    MainTexDim = float2(w,h);
    float2 TextelSize = 1 / MainTexDim;
    float2 sampleUv = UV + dir * TextelSize;
    float alpha;
    if (sampleUv.x < 0 || sampleUv.y < 0 || sampleUv.x > 1 || sampleUv.y > 1)
        alpha = 0;
    else
    {
        alpha = MainTex.Sample(sampler_MainTex,sampleUv).w;
    }
    if (alpha == 0)
    {
        return true;
    }
    return false;
}
float4 CheckCanDissolve(Texture2D CameraTexutre, SamplerState sampler_CameraTexture,Texture2D MainTex,SamplerState sampler_MainTex,float2 UV,float2 ScreenUV,float radius,float ScaleX,float ScaleY,float4 oldCol)
{
    bool inRadius = false;

    float dis = 1;
    
    [loop]
    for (int i = -radius; i <= radius;  i++)
    {
         bool tempcheck =  DistanceCheck(MainTex,sampler_MainTex,UV, float2(0,i));
         tempcheck = tempcheck || DistanceCheck(MainTex,sampler_MainTex,UV, float2(i,0));
        inRadius = inRadius || tempcheck ;
         DistanceCheck(MainTex,sampler_MainTex,UV, float2(i,0));
        if (tempcheck)
        {
            dis = min(dis, abs(i) / radius);
        }
    }
   
    // if (!inRadius)   
    // [loop]
    // for (int j = -radius; j <= radius; j++)
    // {
    //     inRadius = DistanceCheck(MainTex,sampler_MainTex,UV,float2(j,j));
    //     if (inRadius) break;
    //
    // }
    // if (!inRadius)  
    //     [loop]
    //     for (int j = -radius; j <= radius; j++)
    //     {
    //         inRadius = DistanceCheck(MainTex,sampler_MainTex,UV,float2(-j,j));
    //         if (inRadius) break;
    //
    //     }
    // if (!inRadius)  
    //     [loop]
    //     for (int j = -radius; j <= radius; j++)
    //     {
    //         inRadius = DistanceCheck(MainTex,sampler_MainTex,UV,float2(j,-j));
    //         if (inRadius) break;
    //
    //     }
    if (inRadius)
       return Dissolve_Simple_Sin(CameraTexutre,sampler_CameraTexture,ScreenUV,UV,ScaleX,ScaleY,dis) ;
    return oldCol;
}
