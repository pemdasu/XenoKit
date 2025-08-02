using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Gizmo.TransformOperations;
using XenoKit.Engine.Model;
using Xv2CoreLib.EMD;

namespace XenoKit.Engine.Gizmo
{
    public class ModelGizmo : GizmoBase
    {
        protected override Matrix WorldMatrix
        {
            get
            {
                if (transforms == null || transforms.Count == 0) return Matrix.Identity;
                Matrix pos = Matrix.CreateTranslation(centerPosition) * transforms[0].Transform;

                if (attachBone != null)
                    pos *= attachBone.AbsoluteAnimationMatrix;

                return pos;
            }
        }

        protected override ITransformOperation TransformOperation
        {
            get => transformOperation;
            set
            {
                if (value is ModelTransformOperation modelTransformOp)
                {
                    transformOperation = modelTransformOp;
                }
                else
                {
                    transformOperation = null;
                }
            }
        }
        private ModelTransformOperation transformOperation = null;

        private IModelFile SourceModelFile = null;
        private IList<Xv2Submesh> transforms = null;
        private Vector3 centerPosition;
        private Xv2Bone attachBone;

        //Settings
        public override bool AllowRotation => true;
        public override bool AllowScale => true;


        public void SetContext(IModelFile sourceModel, IList<Xv2Submesh> _transforms, Xv2Bone attachBone)
        {
            if (SourceModelFile != null)
                SourceModelFile.ModelModified -= SourceModel_ModelModified;

            SourceModelFile = sourceModel;
            if(SourceModelFile != null)
                SourceModelFile.ModelModified += SourceModel_ModelModified;

            transforms = _transforms;
            CalculateCenter();
            this.attachBone = attachBone;

            base.SetContext();
        }

        private void CalculateCenter()
        {
            centerPosition = Xv2Submesh.CalculateCenter(transforms);
        }

        private void SourceModel_ModelModified(object source, ModelModifiedEventArgs e)
        {
            CalculateCenter();
        }

        public void RemoveContext()
        {
            SetContext(null, null, null);
        }

        public override bool IsContextValid()
        {
            return SourceModelFile != null && transforms != null && SceneManager.CurrentDynamicTab == DynamicTabs.ModelScene;
        }

        protected override void StartTransformOperation()
        {
            if(IsContextValid())
            {
                transformOperation = new ModelTransformOperation(transforms, SourceModelFile);
            }
        }

        public override void OnConfirm()
        {
            CalculateCenter();
        }
    }
}