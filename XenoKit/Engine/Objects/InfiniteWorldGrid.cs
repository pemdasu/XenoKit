using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XenoKit.Engine.Shader;

namespace XenoKit.Engine.Objects
{
    public class InfiniteWorldGrid : EngineObject
    {
        public readonly VertexPosition[] Vertices;
        private readonly PostShaderEffect Shader;

        private const float Near = 0.02f;
        private const float Far = 10f;
        private readonly Vector4 GridColor = new Vector4(0.2f, 0.2f, 0.2f, 1f);

        public InfiniteWorldGrid()
        {
            Shader = CompiledObjectManager.GetCompiledObject<PostShaderEffect>(ShaderManager.GetExtShaderProgram("LB_WorldGrid"));

            Vertices = new VertexPosition[6]
            {
                 new VertexPosition(new Vector3(1, 1, 0)),  new VertexPosition(new Vector3(-1, -1, 0)), new VertexPosition(new Vector3(-1, 1, 0)),
                 new VertexPosition(new Vector3(-1, -1, 0)),  new VertexPosition(new Vector3(1, 1, 0)),  new VertexPosition(new Vector3(1, -1, 0))
            };
            
            Shader.Parameters["near"]?.SetValue(Near);
            Shader.Parameters["far"]?.SetValue(Far);
            Shader.Parameters["gridColor"]?.SetValue(GridColor);
        }

        public override void Draw()
        {
            if (SceneManager.ShowWorldAxis)
            {
                Shader.Parameters["g_mV_VS"]?.SetValue(ViewportInstance.Camera.ViewMatrix);
                Shader.Parameters["g_mP_VS"]?.SetValue(ViewportInstance.Camera.ProjectionMatrix);
                Shader.Parameters["near"]?.SetValue(Near);
                Shader.Parameters["far"]?.SetValue(Far);
                Shader.Parameters["gridColor"]?.SetValue(GridColor);
                Shader.Parameters["supersampleFactor"]?.SetValue(RenderSystem.SuperSampleFactor);

                foreach (var pass in Shader.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
                    GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                    GraphicsDevice.BlendState = BlendState.Additive;

                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, Vertices, 0, 2);
                }
            }
        }
    }
}
