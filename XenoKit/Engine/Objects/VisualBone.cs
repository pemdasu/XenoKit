using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Objects
{
    public class VisualBone : Entity
    {
        private Sphere sphere;
        private BoundingSphere boundingSphere;
        public Matrix4x4 world;

        public bool IsVisible = false;

        //Mesh settings
        private Color DefaultColor = Color.Blue;
        private Color SelectedColor = Color.Red;
        private const float MeshSize = 0.01f;

        public VisualBone(GameBase gameBase) : base(gameBase)
        {
            sphere = new Sphere(gameBase, MeshSize, true);
        }

        public void Draw(Matrix4x4 world, bool isSelected)
        {
            if (IsVisible)
            {
                this.world = world;
                sphere.Draw(world, CameraBase.ViewMatrix, CameraBase.ProjectionMatrix, (isSelected) ? SelectedColor : DefaultColor);
            }
        }

        public bool IsMouseOver()
        {
            boundingSphere = new BoundingSphere(Vector3.Zero, MeshSize);
            boundingSphere = boundingSphere.Transform(world);

            float? value = EngineUtils.IntersectDistance(boundingSphere, Input.MousePosition, GameBase);

            return (value != null && !float.IsNaN(value.Value));
        }
    
    }
}
