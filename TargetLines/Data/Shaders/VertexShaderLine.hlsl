cbuffer ConstantBuffer : register(b0)
{
    matrix ViewProjection;
    float3 CameraPosition;
    float RibbonWidth;
};

struct VertexInputType
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct PixelInputType
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float3 WorldPos : TEXCOORD1;
};

PixelInputType Main(VertexInputType input)
{
    PixelInputType output;
    
    float3 lineDirection = input.Normal;
    //float3 viewDirection = normalize(CameraPosition - input.Position);
    float3 viewDirection = normalize(float3(0, 0, -1) - input.Position);
    float3 sideDirection = normalize(cross(lineDirection, viewDirection));
    
    float3 offset = sideDirection * input.TexCoord.x * RibbonWidth;
    float3 worldPos = input.Position + offset;
    
    output.Position = mul(float4(worldPos, 1.0f), ViewProjection);
    output.TexCoord = float2(input.TexCoord.x + 0.5, input.TexCoord.y);
    output.WorldPos = worldPos;
    
    return output;
}

