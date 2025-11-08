float4 _Color1Gradient;
float _stop1 = 0.5;
float4 _Color2Gradient;
float _stop2 = 0.9;
float4 _Color3Gradient;

float4 CreateGradient(float y)
{
    y = 1-y; 
    if (y < _stop1)
     return lerp(_Color1Gradient,_Color2Gradient, clamp(y / _stop1,0,1));
    if (y <= _stop2 && y >= _stop1)
     return _Color2Gradient;
    if (y > _stop2)
        return lerp(_Color2Gradient, _Color3Gradient, clamp( (y - _stop2) / (1 - _stop2),0,1));
      

    float t = lerp(0,0.5, 1);
    return t;
}
