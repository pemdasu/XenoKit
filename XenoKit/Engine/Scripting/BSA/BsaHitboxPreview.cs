using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using XenoKit.Engine.Collision;
using Xv2CoreLib.BSA;
using Xv2CoreLib.Resource.App;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Scripting.BSA
{
    public class BsaHitboxPreview : EngineObject, IDisposable
    {
        private readonly BSA_Type3 hitbox;
        private readonly Func<Matrix4x4> getDrawMatrix;
        private readonly Func<Matrix4x4> getEndMatrix;
        private readonly Func<int> getFrame;
        private readonly HitboxVisual hitboxVisual;
        private Matrix4x4 growingHitboxMatrix;
        private Vector3 growingHitboxEnd;
        private bool needsRebuild;
        private readonly Dictionary<int, int> hitCounts;

        // The game uses I_48's high byte as a signed counter slot and its low byte as the hit limit.
        private int CounterSlot => (sbyte)(hitbox.I_48 >> 8);
        private int HitLimit => hitbox.I_48 & 0xFF;

        public int GetHitCount()
        {
            return CounterSlot >= 0 && hitCounts.TryGetValue(CounterSlot, out int count) ? count : 0;
        }

        public void RecordHit()
        {
            if (CounterSlot >= 0)
                hitCounts[CounterSlot] = GetHitCount() + 1;
        }

        public ushort GetBdmEntryId()
        {
            int hitCount = GetHitCount();
            if (CounterSlot >= 0 && hitbox.LastHit < 0x8000 && hitCount + 1 == HitLimit)
                return hitbox.LastHit;
            if (hitbox.MultipleHits < 0x8000 && hitCount > 0)
                return hitbox.MultipleHits;
            return hitbox.FirstHit;
        }

        public bool CanHit()
        {
            return CounterSlot < 0 || (HitLimit > 0 && GetHitCount() < HitLimit);
        }

        public bool TryGetBounds(out BoundingBox bounds)
        {
            bounds = new BoundingBox();
            if (hitbox == null || !IsValidForCurrentFrame())
                return false;

            Matrix4x4 matrix = BsaHitboxGeometry.UsesDistanceRelativeGeometry(hitbox)
                ? growingHitboxMatrix : getDrawMatrix();
            SimdVector3 position = new SimdVector3(hitbox.F_08, hitbox.F_12, hitbox.F_16);
            float radius = Math.Abs(hitbox.F_20);
            SimdVector3 min;
            SimdVector3 max;

            switch (hitbox.I_00)
            {
                case 0:
                    if (radius <= 0f) return false;
                    min = max = SimdVector3.Transform(position, matrix);
                    break;
                case 1:
                    if (radius <= 0f) return false;
                    GetCapsuleEndpoints(position, out SimdVector3 start, out SimdVector3 end);
                    min = SimdVector3.Min(SimdVector3.Transform(start, matrix), SimdVector3.Transform(end, matrix));
                    max = SimdVector3.Max(SimdVector3.Transform(start, matrix), SimdVector3.Transform(end, matrix));
                    break;
                case 2:
                    SimdVector3 center = SimdVector3.Transform(position, matrix);
                    SimdVector3 halfExtents = new SimdVector3(Math.Abs(hitbox.F_20), Math.Abs(hitbox.F_24), Math.Abs(hitbox.F_28));
                    SimdVector3 worldExtents = new SimdVector3(
                        Math.Abs(matrix.M11) * halfExtents.X + Math.Abs(matrix.M21) * halfExtents.Y + Math.Abs(matrix.M31) * halfExtents.Z,
                        Math.Abs(matrix.M12) * halfExtents.X + Math.Abs(matrix.M22) * halfExtents.Y + Math.Abs(matrix.M32) * halfExtents.Z,
                        Math.Abs(matrix.M13) * halfExtents.X + Math.Abs(matrix.M23) * halfExtents.Y + Math.Abs(matrix.M33) * halfExtents.Z);
                    min = center - worldExtents;
                    max = center + worldExtents;
                    radius = 0f;
                    break;
                default:
                    return false;
            }

            if (radius > 0f)
            {
                float worldRadius = radius * Math.Max(SimdVector3.TransformNormal(SimdVector3.UnitX, matrix).Length(),
                    Math.Max(SimdVector3.TransformNormal(SimdVector3.UnitY, matrix).Length(), SimdVector3.TransformNormal(SimdVector3.UnitZ, matrix).Length()));
                min -= new SimdVector3(worldRadius);
                max += new SimdVector3(worldRadius);
            }
            if (float.IsNaN(min.X) || float.IsNaN(min.Y) || float.IsNaN(min.Z) ||
                float.IsNaN(max.X) || float.IsNaN(max.Y) || float.IsNaN(max.Z))
                return false;

            bounds = new BoundingBox(Extensions.ToXna(min), Extensions.ToXna(max));
            return true;
        }

        public bool IntersectsSphere(BoundingSphere sphere)
        {
            if (hitbox == null || !IsValidForCurrentFrame())
                return false;

            Matrix4x4 matrix = BsaHitboxGeometry.UsesDistanceRelativeGeometry(hitbox)
                ? growingHitboxMatrix : getDrawMatrix();
            SimdVector3 position = new SimdVector3(hitbox.F_08, hitbox.F_12, hitbox.F_16);
            SimdVector3 sphereCenter = Extensions.ToNumerics(sphere.Center);
            float scaleX = SimdVector3.TransformNormal(SimdVector3.UnitX, matrix).Length();
            float scaleY = SimdVector3.TransformNormal(SimdVector3.UnitY, matrix).Length();
            float scaleZ = SimdVector3.TransformNormal(SimdVector3.UnitZ, matrix).Length();
            float radius = Math.Abs(hitbox.F_20) * Math.Max(scaleX, Math.Max(scaleY, scaleZ)) + sphere.Radius;

            switch (hitbox.I_00)
            {
                case 0:
                    return SimdVector3.DistanceSquared(sphereCenter, SimdVector3.Transform(position, matrix)) <= radius * radius;
                case 1:
                    GetCapsuleEndpoints(position, out SimdVector3 start, out SimdVector3 end);
                    start = SimdVector3.Transform(start, matrix);
                    end = SimdVector3.Transform(end, matrix);
                    SimdVector3 segment = end - start;
                    float segmentLengthSquared = segment.LengthSquared();
                    float nearestPoint = segmentLengthSquared > 0f
                        ? Math.Max(0f, Math.Min(1f, SimdVector3.Dot(sphereCenter - start, segment) / segmentLengthSquared))
                        : 0f;
                    return SimdVector3.DistanceSquared(sphereCenter, start + segment * nearestPoint) <= radius * radius;
                case 2:
                    if (!Matrix4x4.Invert(matrix, out Matrix4x4 inverseMatrix))
                        return false;
                    SimdVector3 localCenter = SimdVector3.Transform(sphereCenter, inverseMatrix) - position;
                    SimdVector3 halfExtents = new SimdVector3(Math.Abs(hitbox.F_20), Math.Abs(hitbox.F_24), Math.Abs(hitbox.F_28));
                    SimdVector3 nearest = SimdVector3.Min(halfExtents, SimdVector3.Max(-halfExtents, localCenter));
                    float minScale = Math.Min(scaleX, Math.Min(scaleY, scaleZ));
                    if (minScale <= 0f)
                        return false;
                    float localRadius = sphere.Radius / minScale;
                    return SimdVector3.DistanceSquared(localCenter, nearest) <= localRadius * localRadius;
                default:
                    return false;
            }
        }

        private void GetCapsuleEndpoints(SimdVector3 position, out SimdVector3 start, out SimdVector3 end)
        {
            if (BsaHitboxGeometry.UsesDistanceRelativeGeometry(hitbox))
            {
                start = position;
                end = new SimdVector3(growingHitboxEnd.X, growingHitboxEnd.Y, growingHitboxEnd.Z);
            }
            else
            {
                start = position + new SimdVector3(hitbox.F_24, hitbox.F_28, hitbox.F_32);
                end = position + new SimdVector3(hitbox.F_36, hitbox.F_40, hitbox.F_44);
            }
        }

        public BsaHitboxPreview(BSA_Type3 hitbox, Func<Matrix4x4> getDrawMatrix, Func<Matrix4x4> getEndMatrix, Func<int> getFrame, Dictionary<int, int> hitCounts)
        {
            this.hitbox = hitbox;
            this.getDrawMatrix = getDrawMatrix;
            this.getEndMatrix = getEndMatrix;
            this.getFrame = getFrame;
            this.hitCounts = hitCounts;
            hitboxVisual = new HitboxVisual(new Color(255, 255, 0, 64), Color.Yellow);

            if (this.hitbox != null)
                this.hitbox.PropertyChanged += Hitbox_PropertyChanged;

            needsRebuild = true;
        }

        public override void Update()
        {
            if (needsRebuild || BsaHitboxGeometry.UsesDistanceRelativeGeometry(hitbox))
                UpdateHitbox();
        }

        private void Hitbox_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            needsRebuild = true;
        }

        public override void Draw()
        {
            if (!IsContextValid())
                return;

            if (needsRebuild)
                UpdateHitbox();

            Matrix4x4 drawMatrix = BsaHitboxGeometry.UsesDistanceRelativeGeometry(hitbox)
                ? growingHitboxMatrix
                : getDrawMatrix();
            hitboxVisual.Draw(Extensions.ToXna(drawMatrix));
        }

        public void Dispose()
        {
            if (hitbox != null)
                hitbox.PropertyChanged -= Hitbox_PropertyChanged;

            hitboxVisual.Dispose();
        }

        private void UpdateHitbox()
        {
            needsRebuild = false;
            hitboxVisual.Clear();

            if (hitbox == null)
                return;

            Vector3 position = new Vector3(hitbox.F_08, hitbox.F_12, hitbox.F_16);

            switch (hitbox.I_00)
            {
                case 0:
                    hitboxVisual.SetSphere(position, Math.Abs(hitbox.F_20));
                    break;
                case 1:
                    SetCapsule(position, Math.Abs(hitbox.F_20));
                    break;
                case 2:
                    Vector3 halfExtents = new Vector3(
                        Math.Abs(hitbox.F_20),
                        Math.Abs(hitbox.F_24),
                        Math.Abs(hitbox.F_28));
                    hitboxVisual.SetBox(position, halfExtents);
                    break;
            }
        }

        private void SetCapsule(Vector3 position, float radius)
        {
            if (BsaHitboxGeometry.UsesDistanceRelativeGeometry(hitbox))
            {
                growingHitboxMatrix = getDrawMatrix();
                growingHitboxEnd = GetDistanceRelativeEnd(position, growingHitboxMatrix, getEndMatrix());
                hitboxVisual.SetCapsule(position, growingHitboxEnd, radius);
                return;
            }

            Vector3 start = position + new Vector3(hitbox.F_24, hitbox.F_28, hitbox.F_32);
            Vector3 end = position + new Vector3(hitbox.F_36, hitbox.F_40, hitbox.F_44);
            hitboxVisual.SetCapsule(start, end, radius);
        }

        internal static Vector3 GetDistanceRelativeEnd(Vector3 position, Matrix4x4 startMatrix, Matrix4x4 endMatrix)
        {
            if (!Matrix4x4.Invert(startMatrix, out Matrix4x4 inverseStart))
                throw new InvalidOperationException("Cannot draw a growing BSA hitbox because its start transform is not invertible.");

            SimdVector3 localPosition = new SimdVector3(position.X, position.Y, position.Z);
            SimdVector3 worldEnd = SimdVector3.Transform(localPosition, endMatrix);
            SimdVector3 localEnd = SimdVector3.Transform(worldEnd, inverseStart);
            return new Vector3(localEnd.X, localEnd.Y, localEnd.Z);
        }

        private bool IsContextValid()
        {
            return hitbox != null && IsValidForCurrentFrame() && SettingsManager.Instance.Settings.XenoKit_HitboxSimulation;
        }

        private bool IsValidForCurrentFrame()
        {
            int frame = getFrame();

            if (frame < hitbox.StartTime)
                return false;

            return (int)hitbox.StartTime + hitbox.Duration > frame;
        }

    }
}
