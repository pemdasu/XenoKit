namespace XenoKit.Engine
{
    public class RenderObject : EngineObject
    {
        public virtual bool DrawThisFrame { get; set; }
        public virtual int LowRezMode => 0;
        public bool IsDestroyed { get; protected set; }

        public void Destroy()
        {
            IsDestroyed = true;
        }
    }
}
