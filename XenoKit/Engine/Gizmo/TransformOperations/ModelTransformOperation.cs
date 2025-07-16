using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Model;
using Xv2CoreLib.Resource;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    public class ModelTransformOperation : TransformOperation
    {
        public override RotationType RotationType => RotationType.EulerAngles;

        private IList<Xv2Submesh> transforms;
        private Matrix[] originalTransforms;

        private Vector3 position = Vector3.Zero;
        private Vector3 scale = Vector3.One;
        private Vector3 rotation = Vector3.Zero;
        private Matrix originalMatrix = Matrix.Identity;

        public ModelTransformOperation(IList<Xv2Submesh> _transforms)
        {
            transforms = _transforms;
            originalTransforms = new Matrix[transforms.Count];

            for(int i = 0; i < transforms.Count; i++)
            {
                originalTransforms[i] = transforms[i].Transform;
            }

            if(originalTransforms[0].Decompose(out Vector3 _scale, out Quaternion _rot, out Vector3 _translation))
            {
                position = _translation;
                scale = _scale;
                rotation = _rot.ToEuler();

                originalMatrix *= Matrix.CreateScale(scale);
                originalMatrix *= Matrix.CreateFromQuaternion(_rot);
                originalMatrix *= Matrix.CreateTranslation(position);
            }
        }

        public override void Confirm()
        {
            if (IsFinished)
                throw new InvalidOperationException($"ModelTransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            //Call into sourceModel to update the source model file
            //Also add an undo step for the transforms

            IsFinished = true;
        }

        public override void Cancel()
        {
            if (IsFinished)
                throw new InvalidOperationException($"ModelTransformOperation.Cancel: This transformation has already been finished, cannot add undo step or cancel at this point.");

            for (int i = 0; i < transforms.Count; i++)
            {
                transforms[i].Transform = originalTransforms[i];
            }

            IsFinished = true;
        }

        public override void UpdatePos(Vector3 delta)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;

                position += new Vector3(-delta.X, delta.Y, delta.Z);
                UpdateTransform();
            }
        }

        public override void UpdateRot(Vector3 newRot)
        {
            if (newRot != rotation)
            {
                Modified = true;
                rotation = newRot;
                UpdateTransform();
            }
        }

        public override void UpdateScale(Vector3 delta)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;
                scale += delta;
                scale.ClampScale();
                UpdateTransform();
            }
        }

        private void UpdateTransform()
        {
            Matrix deltaMatrix = Matrix.Invert(originalMatrix * transforms[0].Parent.Parent.AttachBone.AbsoluteAnimationMatrix);
            deltaMatrix *= Matrix.CreateScale(scale);
            deltaMatrix *= Matrix.CreateFromQuaternion(rotation.EulerToQuaternion());
            deltaMatrix *= Matrix.CreateTranslation(position);
            deltaMatrix *= transforms[0].Parent.Parent.AttachBone.AbsoluteAnimationMatrix;

            for (int i = 0; i < transforms.Count; i++)
            {
                transforms[i].Transform = originalTransforms[i] * deltaMatrix;
            }
        }

        public override Vector3 GetRotationAngles()
        {
            return rotation;
        }
    }
}