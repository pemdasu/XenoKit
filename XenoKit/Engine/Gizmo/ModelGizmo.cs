using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using XenoKit.Engine.Animation;
using XenoKit.Engine.Gizmo.TransformOperations;
using XenoKit.Engine.Model;

namespace XenoKit.Engine.Gizmo
{
    public class ModelGizmo : GizmoBase
    {
        protected override Matrix WorldMatrix
        {
            get
            {
                if (transforms == null) return Matrix.Identity;
                Matrix pos = transforms[0].Transform * Matrix.CreateTranslation(centerPosition);

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

        private IList<Xv2Submesh> transforms = null;
        private Vector3 centerPosition;
        private Xv2Bone attachBone;

        //Settings
        protected override bool AllowRotation => true;
        protected override bool AllowScale => true;

        public ModelGizmo(GameBase gameBase) : base(gameBase)
        {
            
        }

        public void SetContext(IList<Xv2Submesh> _transforms, Xv2Bone attachBone)
        {
            transforms = _transforms;
            centerPosition = Xv2Submesh.CalculateCenter(transforms);
            this.attachBone = attachBone;

            base.SetContext();
        }

        public void RemoveContext()
        {
            SetContext(null, null);
        }

        public override bool IsContextValid()
        {
            return transforms != null && SceneManager.CurrentDynamicTab == DynamicTabs.ModelScene;
        }

        protected override void StartTransformOperation()
        {
            if(IsContextValid())
            {
                transformOperation = new ModelTransformOperation(transforms);
            }
        }
    }
}