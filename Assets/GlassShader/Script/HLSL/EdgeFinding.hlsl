



float LaplacianEdge(Texture2D tex, SamplerState samp, float2 uv,float radius)
{
    // Kernel Laplacian
    float edge = 0.0;
    float w,h;
    tex.GetDimensions(w,h);
    float2 texelSize = 1 / float2(w,h);
    // Duyệt qua kernel 3x3
    float oldAlp = tex.Sample(samp, uv).w;
    
    [loop]
    for (int i = -radius; i <= radius;  i++)
    {
        [loop]
        for (int j = -radius; j <= radius; j++)
        {
            float2 offset = float2(i, j) * texelSize;
            float2 sampleUv = uv + offset;
            float4 col;
            if (sampleUv.x < 0 || sampleUv.y < 0 || sampleUv.x > 1 || sampleUv.y > 1)
                col = 0;
            else
                col = tex.Sample(samp, sampleUv);

            float alp = col.w;
            
            if (alp <= 0.01 && oldAlp == 1)
            {
                edge = 1;
                break;
            }
        }
    }
   
    return edge;
   float t = edge + oldAlp;
}