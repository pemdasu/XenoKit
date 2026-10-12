using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Shapes;

namespace XenoKit.Engine.Objects
{
    public class ObjectAxis : EngineObject
    {
        public Cube yCube;
        public Cube xCube;
        public Cube zCube;
        public Cube leftBoneCube;
        private readonly bool IsWorldAxis;

        public ObjectAxis(bool isWorldAxis)
        {
            IsWorldAxis = isWorldAxis;
            xCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.005f, 0.2f, 0.005f), Color.Red, true, true);
            yCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.005f, 0.2f, 0.005f), Color.Green, true, true);
            zCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.005f, 0.2f, 0.005f), Color.Blue, true, true);
            leftBoneCube = new Cube(new Vector3(0, 0, 0), new Vector3(0.01f, 0.01f, 0.01f), Color.Yellow, false, true);
        }

        public override void Draw()
        {
            Draw(Matrix.Identity);
        }

        public void Draw(Matrix world, bool isLeft = false)
        {
            if((IsWorldAxis && SceneManager.ShowWorldAxis) || !IsWorldAxis)
            {
                Matrix xWorld = Matrix.CreateRotationZ(-(float)Math.PI / 2.0f) * world;
                Matrix zWorld = Matrix.CreateRotationX((float)Math.PI / 2.0f) * world;

                foreach (Matrix projection in RenderSystem.GetLineProjections(Camera.ProjectionMatrix))
                {
                    xCube.Draw(xWorld, projection);
                    yCube.Draw(world, projection);
                    zCube.Draw(zWorld, projection);
                    if (isLeft)
                        leftBoneCube.Draw(world, projection);
                }
            }
        }
    }
}
