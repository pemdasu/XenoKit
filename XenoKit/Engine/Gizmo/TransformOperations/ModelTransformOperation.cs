using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using XenoKit.Engine.Model;
using Xv2CoreLib.EMD;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.UndoRedo;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdQuaternion = System.Numerics.Quaternion;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Gizmo.TransformOperations
{
    public class ModelTransformOperation : TransformOperation
    {
        public override RotationType RotationType => RotationType.EulerAngles;

        private IModelFile SourceModel;
        private IList<Xv2Submesh> transforms;
        private Matrix4x4[] originalTransforms;

        private SimdVector3 position = SimdVector3.Zero;
        private SimdVector3 scale = SimdVector3.One;
        private SimdVector3 rotation = SimdVector3.Zero;
        private Matrix4x4 originalMatrix = Matrix4x4.Identity;

        private SimdVector3 center;

        public ModelTransformOperation(IList<Xv2Submesh> _transforms, IModelFile sourceModelFile)
        {
            SourceModel = sourceModelFile;
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

            center = Xv2Submesh.CalculateCenter(_transforms);
        }

        public override void Confirm()
        {
            if (IsFinished)
                throw new InvalidOperationException($"ModelTransformOperation.Confirm: This transformation has already been finished, cannot add undo step or cancel at this point.");

            //Call into sourceModel to update the source model file
            //Also add an undo step for the transforms
            ApplyTransformation(transforms, SourceModel);

            IsFinished = true;
        }

        public static void ApplyTransformation(IList<Xv2Submesh> transforms, IModelFile SourceModel)
        {
            List<IUndoRedo> undos = new List<IUndoRedo>();
            List<object> sourceSubmeshes = new List<object>();

            foreach (var submesh in transforms)
            {
                submesh.ApplyTransformationToSource(undos);

                if (submesh.SourceEmdSubmesh != null)
                    sourceSubmeshes.Add(submesh.SourceEmdSubmesh);
                else if (submesh.SourceEmgSubmesh != null)
                    sourceSubmeshes.Add(submesh.SourceEmgSubmesh);
            }

            undos.Add(new UndoActionDelegate(SourceModel, nameof(SourceModel.TriggerModelModifiedEvent), true, args: EMD_File.CreateTriggerParams(EditTypeEnum.Vertex, sourceSubmeshes)));
            UndoManager.Instance.AddCompositeUndo(undos, "Model Transform", UndoGroup.EMD);
            SourceModel.TriggerModelModifiedEvent(EditTypeEnum.Vertex, sourceSubmeshes, null);
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
            Matrix4x4 attachMatrix = transforms[0].Parent.Parent.AttachBone != null ? transforms[0].Parent.Parent.AttachBone.AbsoluteAnimationMatrix : Matrix4x4.Identity;

            if (SceneManager.PivotPoint == PivotPoint.Center)
                attachMatrix *= Matrix4x4.CreateTranslation(center);

            Matrix4x4 deltaMatrix = MathHelpers.Invert(originalMatrix * attachMatrix);
            deltaMatrix *= Matrix4x4.CreateScale(scale);
            deltaMatrix *= Matrix4x4.CreateFromQuaternion(rotation.EulerToQuaternion());
            deltaMatrix *= Matrix4x4.CreateTranslation(position);
            deltaMatrix *= attachMatrix;

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