using Microsoft.Xna.Framework;
using System;
using XenoKit.Editor;
using XenoKit.Engine.Scripting.BAC;
using XenoKit.Engine.View;
using Xv2CoreLib.BAC;
using Xv2CoreLib.CBS;
using static Xv2CoreLib.Xenoverse2;

namespace XenoKit.Engine.Gizmo
{
    public class HitboxGizmo : EngineObject
    {
        protected Matrix WorldMatrix
        {
            get
            {
                if (Hitbox == null) return Matrix.Identity;
                Matrix world = Matrix.Identity;

                if (boneIdx != -1 && actor != null)
                {
                    world = actor.GetAbsoluteBoneMatrix(boneIdx);

                    //Hitbox doesn't rotate with b_C_Base, so the rotation needs to be removed
                    if (isBaseBone)
                    {
                        world = Matrix.CreateTranslation(world.Translation);
                    }
                }

                return world;
            }
        }
        private readonly BacHitboxVisual HitboxVisual;

        private BAC_Type1 Hitbox = null;
        private Actor actor;
        private int boneIdx = -1;
        private bool isBaseBone = false;
        private bool RefreshHitbox = false;

        public HitboxGizmo() : base()
        {
            HitboxVisual = new BacHitboxVisual(new Color(255, 0, 0, 64), Color.Red);
            SceneManager.ActorChanged += SceneManager_ActorChanged;
        }

        public override void Update()
        {
            if (RefreshHitbox && IsContextValid())
            {
                SetContext(Hitbox);
                RefreshHitbox = false;
            }
        }

        public override void Draw()
        {
            if (IsContextValid() && actor != null)
            {
                HitboxVisual.Draw(WorldMatrix, actor.Transform);
            }
        }

        public void SetContext(BAC_Type1 hitbox)
        {
            RemoveContext();
            Hitbox = hitbox;

            if(hitbox != null && SceneManager.Actors[0] != null)
            {
                string boneName = hitbox.BoneLink.ToString();
                actor = Collision.BacHitbox.GetSpawnActor(Hitbox, SceneManager.Actors[0]);

                if(actor != null)
                {
                    isBaseBone = boneName == Xv2CoreLib.ESK.ESK_File.BaseBone;
                    boneIdx = actor.Skeleton.GetBoneIndex(boneName);
                    HitboxVisual.Update(hitbox, Collision.BacHitbox.GetSizeScale(actor, hitbox));
                }

                hitbox.PropertyChanged += Hitbox_PropertyChanged;
            }
        }

        public void RemoveContext()
        {
            if(Hitbox != null)
            {
                Hitbox.PropertyChanged -= Hitbox_PropertyChanged;
                Hitbox = null;
                boneIdx = -1;
                isBaseBone = false;
                RefreshHitbox = false;
                actor = null;
            }
        }

        public bool IsContextValid()
        {
            return Hitbox != null && Controls.BacTab.StaticSelectedBacType == Hitbox && SceneManager.CurrentSceneState == EditorTabs.Action && !ViewportInstance.IsPlaying;
        }

        private void Hitbox_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RefreshHitbox = true;
        }

        private void SceneManager_ActorChanged(object source, ActorChangedEventArgs e)
        {
            //Use this to update character bone reference
            if (Hitbox != null)
            {
                SetContext(Hitbox);
            }
        }
    }
}
