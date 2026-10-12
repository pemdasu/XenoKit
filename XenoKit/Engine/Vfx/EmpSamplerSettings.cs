using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Vfx
{
    internal static class EmpSamplerSettings
    {
        public static TextureFilter GetFilter(EMP_TextureSamplerDef.TextureFiltering? min, EMP_TextureSamplerDef.TextureFiltering? mag)
        {
            if (min == EMP_TextureSamplerDef.TextureFiltering.Linear && mag == EMP_TextureSamplerDef.TextureFiltering.Linear)
                return TextureFilter.Linear;

            if (min == EMP_TextureSamplerDef.TextureFiltering.Linear && mag == EMP_TextureSamplerDef.TextureFiltering.Point)
                return TextureFilter.MinLinearMagPointMipLinear;

            if (min == EMP_TextureSamplerDef.TextureFiltering.Point && mag == EMP_TextureSamplerDef.TextureFiltering.Point)
                return TextureFilter.PointMipLinear;

            if (min == EMP_TextureSamplerDef.TextureFiltering.Point && mag == EMP_TextureSamplerDef.TextureFiltering.Linear)
                return TextureFilter.MinPointMagLinearMipLinear;

            return TextureFilter.Linear;
        }

        public static TextureAddressMode GetAddressMode(EMP_TextureSamplerDef.TextureRepitition? mode)
        {
            switch (mode)
            {
                case EMP_TextureSamplerDef.TextureRepitition.Clamp:
                case EMP_TextureSamplerDef.TextureRepitition.Border:
                    return TextureAddressMode.Clamp;
                case EMP_TextureSamplerDef.TextureRepitition.Mirror:
                    return TextureAddressMode.Mirror;
                case EMP_TextureSamplerDef.TextureRepitition.Wrap:
                default:
                    return TextureAddressMode.Wrap;
            }
        }
    }
}
