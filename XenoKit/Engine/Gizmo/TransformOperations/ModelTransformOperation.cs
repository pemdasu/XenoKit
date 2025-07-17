using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Model;
using Xv2CoreLib.Resource;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;
using SimdQuaternion = System.Numerics.Quaternion;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    public class ModelTransformOperation : TransformOperation
    {
        public override RotationType RotationType => RotationType.EulerAngles;

        private IList<Xv2Submesh> transforms;
        private Matrix4x4[] originalTransforms;

        private SimdVector3 position = SimdVector3.Zero;
        private SimdVector3 scale = SimdVector3.One;
        private SimdVector3 rotation = SimdVector3.Zero;
        private Matrix4x4 originalMatrix = Matrix4x4.Identity;

        public ModelTransformOperation(IList<Xv2Submesh> _transforms)
        {
            transforms = _transforms;
            originalTransforms = new Matrix4x4[transforms.Count];

            for(int i = 0; i < transforms.Count; i++)
            {
                originalTransforms[i] = transforms[i].Transform;
            }

            if(Matrix4x4.Decompose(originalTransforms[0], out SimdVector3 _scale, out SimdQuaternion _rot, out SimdVector3 _translation))
            {
                position = _translation;
                scale = _scale;
                rotation = _rot.ToEuler();

                originalMatrix *= Matrix4x4.CreateScale(scale);
                originalMatrix *= Matrix4x4.CreateFromQuaternion(_rot);
                originalMatrix *= Matrix4x4.CreateTranslation(position);
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

                position += new SimdVector3(-delta.X, delta.Y, delta.Z);
                UpdateTransform();
            }
        }

        public override void UpdateRot(Vector3 newRot)
        {
            if (newRot != rotation)
            {
                Modified = true;
                rotation = newRot.ToNumerics();
                UpdateTransform();
            }
        }

        public override void UpdateScale(Vector3 delta)
        {
            if (delta != Vector3.Zero)
            {
                Modified = true;
                scale += delta.ToNumerics();
                scale.ClampScale();
                UpdateTransform();
            }
        }

        private void UpdateTransform()
        {
            Matrix4x4 deltaMatrix = MathHelpers.Invert(originalMatrix * transforms[0].Parent.Parent.AttachBone.AbsoluteAnimationMatrix);
            deltaMatrix *= Matrix4x4.CreateScale(scale);
            deltaMatrix *= Matrix4x4.CreateFromQuaternion(rotation.EulerToQuaternion());
            deltaMatrix *= Matrix4x4.CreateTranslation(position);
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