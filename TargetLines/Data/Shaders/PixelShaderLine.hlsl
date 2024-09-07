struct PixelInputType
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float3 WorldPos : TEXCOORD1;
};

float4 Main(PixelInputType input) : SV_TARGET
{
    float4 baseColor = float4(0.5, 0.7, 1.0, 1.0); // Light blue color
    
    // Simple edge glow effect
    float edgeGlow = 1 - abs(input.TexCoord.x - 0.5) * 2;
    edgeGlow = pow(edgeGlow, 3); // Sharpen the glow
    
    float4 finalColor = baseColor * edgeGlow;
    finalColor.a *= edgeGlow;
    
    return finalColor;
}

