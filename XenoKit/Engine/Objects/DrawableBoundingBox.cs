using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;

namespace XenoKit.Engine.Objects
{
	public class DrawableBoundingBox
    {
		private readonly Cube cube;
        public BoundingBox Bounds { get; private set; }

		public DrawableBoundingBox(GameBase game)
		{
			cube = new Cube(new Vector3(0.5f), new Vector3(-0.5f), new Vector3(0.5f), 0.5f, Color.Pink, true, game);
        }

        public void SetBounds(BoundingBox boundingBox)
        {
            Bounds = boundingBox;
            cube.SetBounds(boundingBox.Min, boundingBox.Max, 0f, true);
        }
		
        public void Draw(Matrix world)
        {
            cube.Transform = world;
            cube.Draw();
        }

        public void Draw(Matrix world, BoundingBox box)
        {
            if (Bounds != box)
                SetBounds(box);

            cube.Transform = world;
            cube.Draw();
        }
	}
}