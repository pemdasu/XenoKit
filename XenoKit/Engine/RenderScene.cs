using XenoKit.Engine.Rendering;

namespace XenoKit.Engine
{
    public class RenderScene : RenderObject
    {
        public RenderPipelineStage RenderPipelineStage { get; set; } = RenderPipelineStage.ModelMain;
    }
}
