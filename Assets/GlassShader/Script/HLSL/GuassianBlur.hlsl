

static float kernel[9] = {
      1,2,1,
      2,4,2,
     1,2,1
};
float _Yf,_Xf;
float3 GaussianBlur(Texture2D _Pass0Output,SamplerState sampler_Pass0Output,
    Texture2D LightLayer,SamplerState sampler_LightLayer ,float4 LightTextureScaleOffset,
    float LightLayerContrast,float LightLayerBright,
    Texture2D GlobalTex,SamplerState sampler_GlobalTex,
    uint3 uv)
{
    float3 col = 0;
    
    // Gaussian weights cho kernel 3x3
    int index = 0;
    uint width, height;
    _Pass0Output.GetDimensions(width, height);
   // float2 texelSize = 1.0 / ;

    int totalvalue = 0;
    
    for (int y =-1; y <= 1; y++)
    {
        
        for (int x = -1; x <= 1; x++)
        {
//            Caculate the offset
            float2 NScreenUv = (uv.xy + uint2(x,y)) / float2(width, height) ;
            uint3 offset = uv + uint3(x, y ,0);
            float3 sample = _Pass0Output.Load(offset).xyz;
  //          Sample output Texture with the offset
            
            if (sample.x == 0 && sample.y == 0 && sample.z == 0 )
            {
                // Sample the light layer
                sample = LightLayer.SampleLevel(sampler_LightLayer,NScreenUv * LightTextureScaleOffset.xy + LightTextureScaleOffset.zw,0).xyz;
                // Caculate the avg of 3 color channel
                sample = (sample.x + sample.y + sample.z )/ 3;
                // Tính contrast của light layer
                sample = LightLayerContrast * (sample - 0.5) + 0.5 + LightLayerBright;
                // clamp sample pixel into the range of (0,1)
                sample += GlobalTex.Load(offset).xyz;
                sample = clamp(sample, 0.0, 1.0);
            }
            
            int Kernel_value = kernel[index];
            col += sample * Kernel_value;
            totalvalue +=  Kernel_value;
            index++;
        }
    }
    col = col / totalvalue;
    return col;
}

float4 GaussianBlurForUV(Texture2D<float4> t,SamplerState state,
    Texture2D LightLayer,SamplerState sampler_LightLayer ,
    float LightLayerContrast,float LightLayerBright,
    Texture2D GlobalTex,SamplerState sampler_GlobalTex,
    float2 uv,int radius)
{
    float4 col = 0;
    
    // Gaussian weights cho kernel 3x3
    int index = 0;
    uint width, height;
    t.GetDimensions(width, height);
    float2 texelSize = 1.0 / float2(width, height);

    float totalvalue = 0;
    
    for (int y =-radius; y <= radius; y++)
    {
        for (int x = -radius; x <= radius; x++)
        {
            float2 offset = uv + float2(x, y) * texelSize;
            float4 sample = t.Sample(state,offset);
            if (sample.x == 0 && sample.y == 0 && sample.z == 0    )
            {
                sample = LightLayer.Sample(sampler_LightLayer,offset);
                sample = (sample.x + sample.y + sample.z )/ 3;
                sample = LightLayerContrast * (sample - 0.5) + 0.5 + LightLayerBright;
                sample = clamp(sample, 0.0, 1.0);
                sample += GlobalTex.Sample(sampler_GlobalTex,offset);
            }
            col += sample ;
            totalvalue += 1;
            index++;//
        }
    }
    col = col /  totalvalue;
    
    return col;
}
