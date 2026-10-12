using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using XenoKit.Engine.Vertex;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Shape
{
    public static partial class EffectShapeMeshBuilder
    {
        private static void SetTbindSurfaceNormals(VertexPositionNormalColorTexture[] vertices, IList<EffectShapePoint> shape, IList<EffectShapeSegment> rows)
        {
            int columnCount = shape.Count;
            bool closed = AreClose(shape[0], shape[columnCount - 1]);
            int sectionCount = closed ? columnCount - 1 : columnCount;

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                int previousRow = Math.Max(0, rowIndex - 1);
                int nextRow = Math.Min(rows.Count - 1, rowIndex + 1);

                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    int sectionIndex = columnIndex % sectionCount;
                    int previousColumn = closed ? (sectionIndex + sectionCount - 1) % sectionCount : Math.Max(0, sectionIndex - 1);
                    int nextColumn = closed ? (sectionIndex + 1) % sectionCount : Math.Min(sectionCount - 1, sectionIndex + 1);
                    Vector3 section = vertices[rowIndex * columnCount + nextColumn].Position - vertices[rowIndex * columnCount + previousColumn].Position;
                    Vector3 length = vertices[nextRow * columnCount + columnIndex].Position - vertices[previousRow * columnCount + columnIndex].Position;
                    SimdVector3 sectionDirection = new SimdVector3(section.X, section.Y, section.Z);
                    SimdVector3 lengthDirection = new SimdVector3(length.X, length.Y, length.Z);

                    if (!TryNormalize(lengthDirection, out lengthDirection))
                        lengthDirection = GetTransformDirection(rows[rowIndex].Transform, SimdVector3.UnitZ);

                    int vertexIndex = rowIndex * columnCount + columnIndex;
                    SimdVector3 normal = SimdVector3.Cross(sectionDirection, lengthDirection);

                    if (TryNormalize(normal, out normal))
                        vertices[vertexIndex].Normal = new Vector3(normal.X, normal.Y, normal.Z);

                    if (TryNormalize(sectionDirection, out sectionDirection))
                        vertices[vertexIndex].Tangent = new Vector3(sectionDirection.X, sectionDirection.Y, sectionDirection.Z);
                }
            }
        }

        //The game draws each span between stored samples as a cubic Hermite spline with subRows extra rows (ETR node byte 0x59).
        //The spline runs on the center and on the two axis points, so each row keeps a smooth frame. Other values blend linearly.
        private static List<EffectShapeSegment> BuildHermiteRows(IList<EffectShapeSegment> samples, int subRows, List<EffectShapeSegment> rows)
        {
            rows.Clear();
            SimdVector3[] centers = GetSamplePoints(samples, SimdVector3.Zero);
            SimdVector3[] xPoints = GetSamplePoints(samples, SimdVector3.UnitX);
            SimdVector3[] yPoints = GetSamplePoints(samples, SimdVector3.UnitY);
            SimdVector3[] centerTangents = GetHermiteTangents(centers);
            SimdVector3[] xTangents = GetHermiteTangents(xPoints);
            SimdVector3[] yTangents = GetHermiteTangents(yPoints);

            for (int i = 0; i < samples.Count - 1; i++)
            {
                rows.Add(CreateInterpolatedTrailSample(samples[i], samples[i], 0f, samples[i].Transform));

                for (int step = 1; step <= subRows; step++)
                {
                    float factor = step / (float)(subRows + 1);
                    SimdVector3 center = Hermite(centers[i], centers[i + 1], centerTangents[i], centerTangents[i + 1], factor);
                    SimdVector3 xAxis = Hermite(xPoints[i], xPoints[i + 1], xTangents[i], xTangents[i + 1], factor) - center;
                    SimdVector3 yAxis = Hermite(yPoints[i], yPoints[i + 1], yTangents[i], yTangents[i + 1], factor) - center;
                    SimdVector3 zAxis = SimdVector3.Lerp(GetTransformDirection(samples[i].Transform, SimdVector3.UnitZ), GetTransformDirection(samples[i + 1].Transform, SimdVector3.UnitZ), factor);
                    Matrix4x4 transform = new Matrix4x4(
                        xAxis.X, xAxis.Y, xAxis.Z, 0f,
                        yAxis.X, yAxis.Y, yAxis.Z, 0f,
                        zAxis.X, zAxis.Y, zAxis.Z, 0f,
                        center.X, center.Y, center.Z, 1f);

                    rows.Add(CreateInterpolatedTrailSample(samples[i], samples[i + 1], factor, transform));
                }
            }

            EffectShapeSegment last = samples[samples.Count - 1];
            rows.Add(CreateInterpolatedTrailSample(last, last, 0f, last.Transform));
            return rows;
        }

        private static SimdVector3[] GetSamplePoints(IList<EffectShapeSegment> samples, SimdVector3 localPoint)
        {
            SimdVector3[] points = new SimdVector3[samples.Count];

            for (int i = 0; i < samples.Count; i++)
                points[i] = SimdVector3.Transform(localPoint, samples[i].Transform);

            return points;
        }

        //Chord weighted tangents, as the game computes them. The ends use the plain difference.
        private static SimdVector3[] GetHermiteTangents(SimdVector3[] points)
        {
            int last = points.Length - 1;
            SimdVector3[] tangents = new SimdVector3[points.Length];
            tangents[0] = points[1] - points[0];
            tangents[last] = points[last] - points[last - 1];

            for (int i = 1; i < last; i++)
            {
                SimdVector3 before = points[i] - points[i - 1];
                SimdVector3 after = points[i + 1] - points[i];
                float total = before.Length() + after.Length();
                float weight = total > 0f ? before.Length() / total : 0.5f;
                tangents[i] = before + ((after - before) * weight);
            }

            return tangents;
        }

        private static SimdVector3 Hermite(SimdVector3 start, SimdVector3 end, SimdVector3 startTangent, SimdVector3 endTangent, float factor)
        {
            float squared = factor * factor;
            float cubed = squared * factor;

            return (start * ((2f * cubed) - (3f * squared) + 1f)) +
                (end * ((-2f * cubed) + (3f * squared))) +
                (startTangent * (cubed - (2f * squared) + factor)) +
                (endTangent * (cubed - squared));
        }

        private static List<EffectShapeSegment> ApplyRenderBudget(IList<EffectShapeSegment> samples, int maxRenderSections, List<EffectShapeSegment> scratch)
        {
            int maxSamples = Math.Max(2, maxRenderSections + 1);

            scratch.Clear();

            if (samples.Count <= maxSamples)
            {
                if (scratch.Capacity < samples.Count)
                    scratch.Capacity = samples.Count;

                for (int i = 0; i < samples.Count; i++)
                    scratch.Add(samples[i]);

                return scratch;
            }

            if (scratch.Capacity < maxSamples)
                scratch.Capacity = maxSamples;

            int lastSourceIndex = samples.Count - 1;
            int lastAddedIndex = -1;

            for (int i = 0; i < maxSamples; i++)
            {
                float sourceIndex = i * lastSourceIndex / (float)(maxSamples - 1);
                int roundedIndex = (int)Math.Round(sourceIndex);

                if (roundedIndex == lastAddedIndex && roundedIndex < lastSourceIndex)
                    roundedIndex++;

                scratch.Add(samples[roundedIndex]);
                lastAddedIndex = roundedIndex;
            }

            if (scratch[scratch.Count - 1] != samples[lastSourceIndex])
                scratch[scratch.Count - 1] = samples[lastSourceIndex];

            return scratch;
        }

        private static EffectShapeSegment CreateInterpolatedTrailSample(EffectShapeSegment current, EffectShapeSegment next, float factor, Matrix4x4 transform)
        {
            return new EffectShapeSegment
            {
                Transform = transform,
                Age = Lerp(current.Age, next.Age, factor),
                CreatedFrame = Lerp(current.CreatedFrame, next.CreatedFrame, factor),
                ExpireFrame = Lerp(current.ExpireFrame, next.ExpireFrame, factor),
                Life = Lerp(current.Life, next.Life, factor),
                Scale = Lerp(current.Scale, next.Scale, factor),
                U = Lerp(current.U, next.U, factor),
                V = Lerp(current.V, next.V, factor),
                UvBaseU = Lerp(current.UvBaseU, next.UvBaseU, factor),
                UvBaseV = Lerp(current.UvBaseV, next.UvBaseV, factor),
                NormalizedTrailPosition = Lerp(current.NormalizedTrailPosition, next.NormalizedTrailPosition, factor),
                DistanceFromTail = Lerp(current.DistanceFromTail, next.DistanceFromTail, factor),
                DistanceFromHead = Lerp(current.DistanceFromHead, next.DistanceFromHead, factor),
                TrailLength = Lerp(current.TrailLength, next.TrailLength, factor),
                AlphaScale = Lerp(current.AlphaScale, next.AlphaScale, factor),
                IsBootstrapSeed = current.IsBootstrapSeed && next.IsBootstrapSeed,
                IsRenderOnlyHead = current.IsRenderOnlyHead || next.IsRenderOnlyHead,
                PrimaryColor = Color.Lerp(current.PrimaryColor, next.PrimaryColor, factor),
                SecondaryColor = Color.Lerp(current.SecondaryColor, next.SecondaryColor, factor)
            };
        }

        //Each row faces the camera from its own position, as the game re-faces every segment each frame.
        private static TbindRowFrame[] BuildAutoOrientedRowFrames(IList<EffectShapeSegment> rows, SimdVector3 cameraPosition)
        {
            TbindRowFrame[] frames = new TbindRowFrame[rows.Count];
            SimdVector3[] centers = new SimdVector3[rows.Count];
            SimdVector3 previousTangent = SimdVector3.Zero;
            SimdVector3 previousWidthAxis = SimdVector3.Zero;

            for (int i = 0; i < rows.Count; i++)
                centers[i] = rows[i].Transform.Translation;

            for (int i = 0; i < rows.Count; i++)
            {
                EffectShapeSegment row = rows[i];
                SimdVector3 tangent = GetTrailTangentBasis(rows, centers, i, previousTangent);
                SimdVector3 viewForward = NormalizeOrFallback(cameraPosition - centers[i], SimdVector3.UnitZ);
                SimdVector3 widthAxis = GetAutoOrientedWidthAxis(row.Transform, tangent, viewForward, previousWidthAxis);
                SimdVector3 heightAxis = SimdVector3.Cross(widthAxis, tangent);

                if (!TryNormalize(heightAxis, out heightAxis))
                    heightAxis = GetTransformDirection(row.Transform, SimdVector3.UnitY);

                if (!TryNormalize(heightAxis, out heightAxis))
                    heightAxis = SimdVector3.UnitY;

                SimdVector3 center = centers[i];
                SimdVector3 normal = NormalizeOrFallback(heightAxis, viewForward);

                frames[i] = new TbindRowFrame(center, widthAxis, heightAxis, normal);
                previousTangent = tangent;
                previousWidthAxis = widthAxis;
            }

            return frames;
        }

        private static SimdVector3 GetTrailTangentBasis(IList<EffectShapeSegment> rows, IList<SimdVector3> centers, int rowIndex, SimdVector3 previousTangent)
        {
            SimdVector3 tangent;

            if (rowIndex == 0)
                tangent = centers[1] - centers[0];
            else if (rowIndex == rows.Count - 1)
                tangent = centers[rowIndex] - centers[rowIndex - 1];
            else
                tangent = centers[rowIndex + 1] - centers[rowIndex - 1];

            if (TryNormalize(tangent, out tangent))
                return tangent;

            if (previousTangent.LengthSquared() > MinLengthSquared)
                return previousTangent;

            tangent = GetTransformDirection(rows[rowIndex].Transform, SimdVector3.UnitY);
            return TryNormalize(tangent, out tangent) ? tangent : SimdVector3.UnitY;
        }

        private static SimdVector3 GetAutoOrientedWidthAxis(Matrix4x4 transform, SimdVector3 tangent, SimdVector3 cameraViewForward, SimdVector3 previousWidthAxis)
        {
            SimdVector3 widthAxis = SimdVector3.Cross(tangent, cameraViewForward);

            if (widthAxis.LengthSquared() > 0.0001f)
            {
                widthAxis = SimdVector3.Normalize(widthAxis);

                if (previousWidthAxis.LengthSquared() > MinLengthSquared)
                {
                    if (SimdVector3.Dot(widthAxis, previousWidthAxis) < 0f)
                        widthAxis = -widthAxis;
                }

                return widthAxis;
            }

            if (TryGetProjectedPreviousWidth(tangent, previousWidthAxis, out widthAxis))
                return widthAxis;

            widthAxis = GetTransformDirection(transform, SimdVector3.UnitX);

            return TryNormalize(widthAxis, out widthAxis) ? widthAxis : SimdVector3.UnitX;
        }

        private static bool TryGetProjectedPreviousWidth(SimdVector3 tangent, SimdVector3 previousWidthAxis, out SimdVector3 widthAxis)
        {
            if (previousWidthAxis.LengthSquared() <= MinLengthSquared)
            {
                widthAxis = SimdVector3.Zero;
                return false;
            }

            widthAxis = previousWidthAxis - (tangent * SimdVector3.Dot(previousWidthAxis, tangent));
            return TryNormalize(widthAxis, out widthAxis);
        }

        private static SimdVector3 GetAutoOrientedPoint(EffectShapePoint point, float scale, int columnIndex, int columnCount, TbindRowFrame rowFrame)
        {
            if (columnCount == 2)
                return rowFrame.Center + (rowFrame.WidthAxis * point.X * scale);

            return rowFrame.Center + (rowFrame.WidthAxis * point.X * scale) + (rowFrame.HeightAxis * point.Y * scale);
        }

        private static SimdVector3 NormalizeOrFallback(SimdVector3 value, SimdVector3 fallback)
        {
            return TryNormalize(value, out SimdVector3 normalized) ? normalized : fallback;
        }

        private static bool TryNormalize(SimdVector3 value, out SimdVector3 normalized)
        {
            if (value.LengthSquared() < MinLengthSquared)
            {
                normalized = SimdVector3.Zero;
                return false;
            }

            normalized = SimdVector3.Normalize(value);
            return true;
        }

        private static SimdVector3 GetTransformDirection(Matrix4x4 transform, SimdVector3 direction)
        {
            return SimdVector3.TransformNormal(direction, transform);
        }

        private static void SetTrailDistances(IList<EffectShapeSegment> samples)
        {
            if (samples == null || samples.Count == 0)
                return;

            float totalDistance = 0f;
            samples[0].DistanceFromTail = 0f;

            for (int i = 1; i < samples.Count; i++)
            {
                totalDistance += SimdVector3.Distance(samples[i - 1].Transform.Translation, samples[i].Transform.Translation);
                samples[i].DistanceFromTail = totalDistance;
            }

            for (int i = 0; i < samples.Count; i++)
            {
                samples[i].TrailLength = totalDistance;
                samples[i].DistanceFromHead = totalDistance - samples[i].DistanceFromTail;
            }
        }

        private struct TbindRowFrame
        {
            public SimdVector3 Center { get; }
            public SimdVector3 WidthAxis { get; }
            public SimdVector3 HeightAxis { get; }
            public SimdVector3 Normal { get; }

            public TbindRowFrame(SimdVector3 center, SimdVector3 widthAxis, SimdVector3 heightAxis, SimdVector3 normal)
            {
                Center = center;
                WidthAxis = widthAxis;
                HeightAxis = heightAxis;
                Normal = normal;
            }
        }

    }
}
