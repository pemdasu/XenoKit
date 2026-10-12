// BPE color filter. The rows map input red, green and blue to the output color, and the last row is added.
// Compile: fxc /T ps_5_0 /E main /Fo ColorMatrix.xpu ColorMatrix.hlsl
cbuffer ps_post_process : register(b0)
{
    float4 afRGBA_Modulate[4];
};

Texture2D atex2D_Texture : register(t0);
SamplerState asamp2D_Texture_s : register(s0);

float4 main(float4 position : SV_POSITION, float2 uv : TEXCOORD0) : SV_TARGET
{
    float4 color = atex2D_Texture.Sample(asamp2D_Texture_s, uv);
    float3 rgb = color.r * afRGBA_Modulate[0].rgb + color.g * afRGBA_Modulate[1].rgb + color.b * afRGBA_Modulate[2].rgb + afRGBA_Modulate[3].rgb;
    return float4(rgb, color.a);
}
