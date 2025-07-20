using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Shapes
{
    public class Plane : RenderObject
    {
        public SimdVector3 Size;
        private Color color;

        public VertexPositionColorTexture[] _vertices { get; set; }
        private BasicEffect effect;

        public Plane(SimdVector3 position, SimdVector3 size, Color _color)
        {
            color = _color;
            Size = size;
            effect = new BasicEffect(GraphicsDevice);
            effect.Alpha = 1f;
            effect.VertexColorEnabled = true;
            Transform = Matrix4x4.CreateWorld(position, MathHelpers.Forward, MathHelpers.Up);
            Name = "Cube";
            ConstructPlane();
        }

        private void ConstructPlane()
        {
            _vertices = new VertexPositionColorTexture[6];

            Vector3 topLeftFront = new Vector3(-0.5f, 0.5f, -0.5f) * Size;
            Vector3 topLeftBack = new Vector3(-0.5f, 0.5f, 0.5f) * Size;
            Vector3 topRightFront = new Vector3(0.5f, 0.5f, -0.5f) * Size;
            Vector3 topRightBack = new Vector3(0.5f, 0.5f, 0.5f) * Size;
            
            Vector2 textureTopLeft = new Vector2(1.0f * Size.X, 0.0f * Size.Y);
            Vector2 textureTopRight = new Vector2(0.0f * Size.X, 0.0f * Size.Y);
            Vector2 textureBottomLeft = new Vector2(1.0f * Size.X, 1.0f * Size.Y);
            Vector2 textureBottomRight = new Vector2(0.0f * Size.X, 1.0f * Size.Y);


            _vertices[0] = new VertexPositionColorTexture(topLeftFront, color, textureBottomLeft);
            _vertices[1] = new VertexPositionColorTexture(topRightBack, color, textureTopRight);
            _vertices[2] = new VertexPositionColorTexture(topLeftBack, color, textureTopLeft);
            _vertices[3] = new VertexPositionColorTexture(topLeftFront, color, textureBottomLeft);
            _vertices[4] = new VertexPositionColorTexture(topRightFront, color, textureBottomRight);
            _vertices[5] = new VertexPositionColorTexture(topRightBack, color, textureTopRight);


        }

        public override void Draw()
        {
            effect.Projection = Camera.ProjectionMatrix;
            effect.View = Camera.ViewMatrix;
            effect.World = Transform;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                GraphicsDevice.BlendState = BlendState.Opaque;
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                pass.Apply();

                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _vertices, 0, _vertices.Length / 3);
            }
        }

    }
}
