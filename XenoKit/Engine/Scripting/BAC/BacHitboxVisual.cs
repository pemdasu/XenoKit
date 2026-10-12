using System;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Collision;
using Xv2CoreLib.BAC;

namespace XenoKit.Engine.Scripting.BAC
{
    public class BacHitboxVisual : HitboxVisual
    {
        private BAC_Type1 hitbox;
        private float sizeScale;

        public BacHitboxVisual(Color fillColor, Color wireColor)
            : base(fillColor, wireColor)
        {
        }

        public void Update(BAC_Type1 hitbox, float cbsScaling)
        {
            Clear();
            this.hitbox = hitbox;
            sizeScale = cbsScaling;

            if (hitbox == null)
                return;

            Vector3 position = new Vector3(hitbox.PositionX, hitbox.PositionY, hitbox.PositionZ) * cbsScaling;

            switch (HitShapes.GetShapeType((int)hitbox.BoundingBoxType))
            {
                case HitShapeType.Sphere:
                    SetSphere(position, Math.Abs(hitbox.Size * cbsScaling));
                    break;
                case HitShapeType.Capsule:
                    Vector3 start = new Vector3(
                        (hitbox.PositionX + hitbox.MinX) * cbsScaling,
                        (hitbox.PositionY + hitbox.MinY) * cbsScaling,
                        (hitbox.PositionZ + hitbox.MinZ) * cbsScaling);
                    Vector3 end = new Vector3(
                        (hitbox.PositionX + hitbox.MaxX) * cbsScaling,
                        (hitbox.PositionY + hitbox.MaxY) * cbsScaling,
                        (hitbox.PositionZ + hitbox.MaxZ) * cbsScaling);
                    SetCapsule(start, end, Math.Abs(hitbox.Size * cbsScaling));
                    break;
                case HitShapeType.Box:
                    Vector3 halfExtents = new Vector3(
                        hitbox.Size,
                        hitbox.MinX,
                        hitbox.MinY) * cbsScaling;
                    SetBox(Vector3.Zero, halfExtents);
                    break;
            }
        }

        public void Draw(Matrix boneWorld, Matrix actorWorld)
        {
            if (hitbox != null && HitShapes.GetShapeType((int)hitbox.BoundingBoxType) == HitShapeType.Box)
                base.Draw(BacHitbox.GetBoxWorld(hitbox, boneWorld.ToNumerics(), actorWorld.ToNumerics(), sizeScale));
            else
                base.Draw(boneWorld);
        }

    }
}
