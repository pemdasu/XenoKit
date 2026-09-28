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
            originalMatrix = entity is Actor actor ? actor.BaseTransform : entity.Transform;
        }

        public override void Confirm()
        {
            if (IsFinished)
                throw new InvalidOperationException($"EntityTransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            if (Modified && entity is Actor actor && actor == SceneManager.Actors[1])
                SceneManager.SetVictimTransform(actor.BaseTransform);

            IsFinished = true;
        }

        public override void Cancel()
        {
            if (IsFinished)
                throw new InvalidOperationException($"EntityTransformOperation.Cancel: This transformation has already been finished, cannot add undo step or cancel at this point.");

            if (entity is Actor actor)
                actor.BaseTransform = originalMatrix;
            else
                entity.Transform = originalMatrix;

            IsFinished = true;
        }

        public override void UpdatePos(Vector3 delta)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;
                Matrix4x4 translation = Matrix4x4.CreateTranslation(new SimdVector3(delta.X, delta.Y, delta.Z));
                if (entity is Actor actor)
                    actor.BaseTransform *= translation;
                else
                    entity.Transform *= translation;
            }
        }

        public override Vector3 GetRotationAngles()
        {
            Matrix4x4 matrix = entity is Actor actor ? actor.BaseTransform : entity.Transform;
            Matrix4x4.Decompose(matrix, out SimdVector3 scale, out System.Numerics.Quaternion rotation, out SimdVector3 position);
            return Extensions.ToXna(rotation.ToEuler());
        }

        public override void UpdateRot(Vector3 newRot)
        {
            if (entity is Actor actor && actor == SceneManager.Actors[1])
            {
                Modified = true;
                actor.BaseTransform = Matrix4x4.CreateFromQuaternion(newRot.ToNumerics().EulerToQuaternion()) *
                    Matrix4x4.CreateTranslation(actor.BaseTransform.Translation);
            }
        }
    }
}
