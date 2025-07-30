using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Objects
{
    public class VisualBone : EngineObject
    {
        private Sphere sphere;
        private BoundingSphere boundingSphere;
        public Matrix4x4 world;

        public bool IsVisible = false;

        //Mesh settings
        private Color DefaultColor = Color.Blue;
        private Color SelectedColor = Color.Red;
        private const float MeshSize = 0.02f;

        public VisualBone()
        {
            sphere = new Sphere(MeshSize, true);
        }

        public void Draw(Matrix4x4 world, bool isSelected)
        {
            if (IsVisible)
            {
                this.world = world;
                sphere.Draw(world, Camera.ViewMatrix, Camera.ProjectionMatrix, (isSelected) ? SelectedColor : DefaultColor);
            }
        }

        public bool IsMouseOver()
        {
            boundingSphere = new BoundingSphere(Vector3.Zero, MeshSize);
            boundingSphere = boundingSphere.Transform(world);

            float? value = EngineUtils.IntersectDistance(boundingSphere, Input.MousePosition);

            return (value != null && !float.IsNaN(value.Value));
        }

        public bool IsMouseOver(Matrix4x4 world)
        {
            var aabb = new BoundingBox(new Vector3(-MeshSize), new Vector3(MeshSize));
            aabb = aabb.Transform(world);
            float? value = EngineUtils.IntersectDistance(aabb, Input.MousePosition);

            return (value != null && !float.IsNaN(value.Value));
        }

    }
}
