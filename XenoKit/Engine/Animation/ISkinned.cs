using XenoKit.Controls;

namespace XenoKit.Engine.Animation
{
    public interface ISkinned
    {
        AnimationTabView AnimationViewInstance { get; }
        Xv2Skeleton Skeleton { get; }
        AnimationPlayer AnimationPlayer { get; }

        System.Numerics.Matrix4x4 GetAbsoluteBoneMatrix(int boneIdx);
    }
}
