using Microsoft.Xna.Framework;
using XenoKit.Engine.Gizmo.TransformOperations;

namespace XenoKit.Engine.Gizmo
{
    public class EntityTransformGizmo : GizmoBase
    {
        protected override Matrix WorldMatrix => Entity.Transform;

        public EngineObject Entity;
        public EditorTabs ContextTab;

        private EntityTransformOperation transformOperation = null;
        protected override ITransformOperation TransformOperation
        {
            get => transformOperation;
            set
            {
                if (value is EntityTransformOperation transformOp)
                {
                    transformOperation = transformOp;
                }
                else
                {
                    transformOperation = null;
                }
            }
        }

        public override bool AllowScale => false;
        public override bool AllowRotation => false;
        public override bool AllowTranslate => true;


        public void SetContext(EngineObject entity, EditorTabs contextTab)
        {
            Entity = entity;
            ContextTab = contextTab;
        }

        public override bool IsContextValid()
        {
            return Entity != null && SceneManager.IsOnTab(ContextTab);
        }


        protected override void StartTransformOperation()
        {
            if (IsContextValid())
            {
                transformOperation = new EntityTransformOperation(Entity);
            }
        }
    }
}
