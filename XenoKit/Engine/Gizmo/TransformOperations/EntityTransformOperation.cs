using Microsoft.Xna.Framework;
using System;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    public class EntityTransformOperation : TransformOperation
    {
        private EngineObject entity;
        private Matrix4x4 originalMatrix;

        public EntityTransformOperation(EngineObject entity)
        {
            this.entity = entity;
            originalMatrix = entity.Transform;
        }

        public override void Confirm()
        {
            if (IsFinished)
                throw new InvalidOperationException($"EntityTransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            IsFinished = true;
        }

        public override void Cancel()
        {
            if (IsFinished)
                throw new InvalidOperationException($"EntityTransformOperation.Cancel: This transformation has already been finished, cannot add undo step or cancel at this point.");

            entity.Transform = originalMatrix;

            IsFinished = true;
        }

        public override void UpdatePos(Vector3 delta)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;
                entity.Transform *= Matrix4x4.CreateTranslation(new SimdVector3(-delta.X, delta.Y, delta.Z));
            }
        }
    }
}
