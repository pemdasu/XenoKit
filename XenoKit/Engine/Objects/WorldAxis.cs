using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;

namespace XenoKit.Engine.Objects
{
    public class WorldAxis : RenderObject
    {
        public Cube oCube;
        public Cube yCube;
        public Cube xCube;
        public Cube zCube;

        public WorldAxis()
        {
            oCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.1f, 0.1f, 0.1f), Color.White);
            xCube = new Cube(new Vector3(1, 0, 0), new Vector3(0.1f, 0.1f, 0.1f), Color.Red);
            yCube = new Cube(new Vector3(0, 1, 0), new Vector3(0.1f, 0.1f, 0.1f), Color.Green);
            zCube = new Cube(new Vector3(0, 0, 1), new Vector3(0.1f, 0.1f, 0.1f), Color.Blue);
        }

        public override void Draw()
        {
            if (SceneManager.ShowWorldAxis)
            {
                oCube.Draw(Transform);
                xCube.Draw(Transform);
                yCube.Draw(Transform);
                zCube.Draw(Transform);
            }
        }

    }
}
