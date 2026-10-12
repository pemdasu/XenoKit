using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using XenoKit.Engine.Vertex;
using XenoKit.Engine.Vfx.Particle;
using Matrix4x4 = System.Numerics.Matrix4x4;
using SimdVector3 = System.Numerics.Vector3;

namespace XenoKit.Engine.Vfx.Shape
{
    internal enum ShapeDrawStripMode
    {
        UprightWidth,
        PathNormalWidth,
        AxialBand,
        PathNormalGroundBand
    }

    public static partial class EffectShapeMeshBuilder
    {
        private const float MinLengthSquared = 0.000001f;
        private const float MiterLimit = 4f;

        internal static EffectShapeMeshData BuildShapeDrawRibbonMesh(IList<EffectShapePoint> points, Matrix4x4 world, float halfWidth, float depthWidth, float innerRadius, float outerRadius, ParticleUvMap primaryMap, ParticleUvMap secondaryMap, Color topColor, Color bottomColor, bool closed, ShapeDrawStripMode mode)
        {
            if (points == null || points.Count < 2)
                return new EffectShapeMeshData(new VertexPositionTextureColor[0], null, PrimitiveType.TriangleStrip, 0);

            bool sourceCloses = closed && points.Count > 2 && AreClose(points[0], points[points.Count - 1]);
            int pointCount = sourceCloses ? points.Count - 1 : points.Count;
            bool isClosed = sourceCloses && pointCount > 2;

            if (pointCount < 2)
                return new EffectShapeMeshData(new VertexPositionTextureColor[0], null, PrimitiveType.TriangleStrip, 0);

            float[] distances = new float[pointCount];
            for (int index = 1; index < pointCount; index++)
            {
                float deltaX = points[index].X - points[index - 1].X;
                float deltaY = points[index].Y - points[index - 1].Y;
                distances[index] = distances[index - 1] + (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            }

            float totalDistance = distances[pointCount - 1];
            if (isClosed)
            {
                float deltaX = points[0].X - points[pointCount - 1].X;
                float deltaY = points[0].Y - points[pointCount - 1].Y;
                totalDistance += (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            }
            bool axialBand = mode == ShapeDrawStripMode.AxialBand;
            float width = axialBand ? halfWidth : Math.Max(0.0001f, halfWidth);
            int pairCount = isClosed ? pointCount + 1 : pointCount;
            List<VertexPositionTextureColor> vertices = new List<VertexPositionTextureColor>((pairCount * 2) + 2);
            VertexPositionTextureColor lastBottom = default(VertexPositionTextureColor);

            for (int i = 0; i < pointCount; i++)
            {
                GetShapeDrawPair(points, pointCount, i, isClosed, width, depthWidth, innerRadius, outerRadius, mode, out SimdVector3 topPosition, out SimdVector3 bottomPosition);
                float fraction = SafeDivide(distances[i], totalDistance);
                Vector2 topUv = primaryMap.Map(new Vector2(fraction, 0f));
                Vector2 bottomUv = primaryMap.Map(new Vector2(fraction, 1f));
                VertexPositionTextureColor top = CreateVertex(topPosition, world, topColor, topUv.X, topUv.Y);
                VertexPositionTextureColor bottom = CreateVertex(bottomPosition, world, bottomColor, bottomUv.X, bottomUv.Y);
                top.TextureUV1 = secondaryMap.Map(new Vector2(fraction, 0f));
                bottom.TextureUV1 = secondaryMap.Map(new Vector2(fraction, 1f));

                vertices.Add(top);
                vertices.Add(bottom);
                lastBottom = bottom;
            }

            if (isClosed)
            {
                GetShapeDrawPair(points, pointCount, 0, isClosed, width, depthWidth, innerRadius, outerRadius, mode, out SimdVector3 topPosition, out SimdVector3 bottomPosition);
                Vector2 topUv = primaryMap.Map(Vector2.UnitX);
                Vector2 bottomUv = primaryMap.Map(Vector2.One);
                VertexPositionTextureColor top = CreateVertex(topPosition, world, topColor, topUv.X, topUv.Y);
                VertexPositionTextureColor bottom = CreateVertex(bottomPosition, world, bottomColor, bottomUv.X, bottomUv.Y);
                top.TextureUV1 = secondaryMap.Map(Vector2.UnitX);
                bottom.TextureUV1 = secondaryMap.Map(Vector2.One);

                vertices.Add(top);
                vertices.Add(bottom);
                lastBottom = bottom;
            }

            vertices.Add(lastBottom);
            vertices.Add(lastBottom);

            return new EffectShapeMeshData(vertices.ToArray(), null, PrimitiveType.TriangleStrip, Math.Max(0, vertices.Count - 2));
        }

        private static void GetShapeDrawPair(IList<EffectShapePoint> points, int pointCount, int index, bool isClosed, float width, float depthWidth, float innerRadius, float outerRadius, ShapeDrawStripMode mode, out SimdVector3 topPosition, out SimdVector3 bottomPosition)
        {
            SimdVector3 center;
            SimdVector3 offset;

            if (mode == ShapeDrawStripMode.AxialBand)
            {
                EffectShapePoint point = points[index];
                topPosition = new SimdVector3(point.Y * innerRadius, point.X * innerRadius, width * 2f);
                bottomPosition = new SimdVector3(point.Y * outerRadius, point.X * outerRadius, 0f);
                return;
            }

            if (mode == ShapeDrawStripMode.PathNormalGroundBand)
            {
                center = new SimdVector3(points[index].X, 0f, points[index].Y);
                SimdVector3 joinOffset = GetJoinOffset(points, pointCount, index, isClosed, width);
                offset = new SimdVector3(joinOffset.X, 0f, joinOffset.Y);
                SimdVector3 depth = new SimdVector3(0f, depthWidth, 0f);
                topPosition = center + offset + (depth * 0.5f);
                bottomPosition = center - offset - (depth * 0.5f);
                return;
            }

            if (mode == ShapeDrawStripMode.PathNormalWidth)
            {
                center = ToVector(points[index]);
                offset = GetJoinOffset(points, pointCount, index, isClosed, width);
                topPosition = center + offset;
                bottomPosition = center - offset;
                return;
            }

            center = new SimdVector3(points[index].X, 0f, points[index].Y);
            offset = new SimdVector3(0f, width, 0f);
            topPosition = center + offset;
            bottomPosition = center - offset;
        }

        internal static EffectShapeMeshData BuildConeExtrudeRibbonMesh(IList<EffectRibbonPoint> points, Matrix4x4 world, ParticleUvMap primaryMap, ParticleUvMap secondaryMap)
        {
            if (points == null || points.Count < 2)
                return new EffectShapeMeshData(new VertexPositionTextureColor[0], null, PrimitiveType.TriangleStrip, 0);

            List<VertexPositionTextureColor> vertices = new List<VertexPositionTextureColor>((points.Count * 2) + 2);
            VertexPositionTextureColor lastBottom = default(VertexPositionTextureColor);

            for (int i = 0; i < points.Count; i++)
            {
                EffectRibbonPoint point = points[i];
                float topWidth = Math.Max(0.0001f, point.HalfWidth);
                float bottomWidth = Math.Max(0.0001f, point.BottomWidth);
                Vector2 topUv = primaryMap.Map(new Vector2(0f, point.U));
                Vector2 bottomUv = primaryMap.Map(new Vector2(1f, point.U));
                VertexPositionTextureColor top = CreateVertex(point.Position + new SimdVector3(topWidth, 0f, 0f), world, point.TopColor, topUv.X, topUv.Y);
                VertexPositionTextureColor bottom = CreateVertex(point.Position - new SimdVector3(bottomWidth, 0f, 0f), world, point.BottomColor, bottomUv.X, bottomUv.Y);
                top.TextureUV1 = secondaryMap.Map(new Vector2(0f, point.U));
                bottom.TextureUV1 = secondaryMap.Map(new Vector2(1f, point.U));

                vertices.Add(top);
                vertices.Add(bottom);
                lastBottom = bottom;
            }

            vertices.Add(lastBottom);
            vertices.Add(lastBottom);

            return new EffectShapeMeshData(vertices.ToArray(), null, PrimitiveType.TriangleStrip, Math.Max(0, vertices.Count - 2));
        }

        internal static EffectShapeMeshData BuildTbindTrailMesh(IList<EffectShapePoint> shape, IList<EffectShapeSegment> samples, int subRows, int maxRenderSections, bool autoOrientation, SimdVector3 cameraPosition, float uvScrollU, float uvScrollV, float uvStepU, float uvStepV, List<EffectShapeSegment> budgetScratch, List<EffectShapeSegment> rowScratch)
        {
            if (shape == null || shape.Count < 2 || samples == null || samples.Count < 2)
                return CreateEmptyTbindMesh();

            List<EffectShapeSegment> baseRows = ApplyRenderBudget(samples, maxRenderSections, budgetScratch);
            int maxRows = ushort.MaxValue / shape.Count;
            subRows = Math.Max(0, Math.Min(subRows, ((maxRows - 1) / (baseRows.Count - 1)) - 1));
            List<EffectShapeSegment> profiledSamples = BuildHermiteRows(baseRows, subRows, rowScratch);

            SetTrailDistances(profiledSamples);

            int rowCount = profiledSamples.Count;
            int columnCount = shape.Count;
            int vertexCount = rowCount * columnCount;

            if (vertexCount > ushort.MaxValue)
                throw new InvalidOperationException($"TBIND strip mesh has {vertexCount} vertices, which exceeds the 16-bit index limit.");

            VertexPositionNormalColorTexture[] vertices = new VertexPositionNormalColorTexture[vertexCount];
            TbindRowFrame[] rowFrames = autoOrientation ? BuildAutoOrientedRowFrames(profiledSamples, cameraPosition) : null;

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                EffectShapeSegment row = profiledSamples[rowIndex];

                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    int vertexIndex = (rowIndex * columnCount) + columnIndex;
                    Vector2 uv = GetTbindGridUv(shape, profiledSamples[0], rowIndex, rowCount, columnIndex, columnCount, uvScrollU, uvScrollV, uvStepU, uvStepV);
                    Color color = GetTbindColumnColor(row, columnIndex, columnCount);
                    SimdVector3 normal = GetTbindVertexNormal(row, rowFrames, rowIndex, autoOrientation);
                    SimdVector3 tangent = GetTbindVertexTangent(row, rowFrames, rowIndex, autoOrientation);

                    if (autoOrientation)
                    {
                        SimdVector3 point = GetAutoOrientedPoint(shape[columnIndex], row.Scale, columnIndex, columnCount, rowFrames[rowIndex]);
                        vertices[vertexIndex] = CreateTrailVertex(point, normal, tangent, color, uv.X, uv.Y);
                    }
                    else
                    {
                        SimdVector3 point = SimdVector3.Transform(ToVector(Scale(shape[columnIndex], row.Scale)), row.Transform);
                        vertices[vertexIndex] = CreateTrailVertex(point, normal, tangent, color, uv.X, uv.Y);
                    }
                }
            }

            if (columnCount > 2)
            {
                SetTbindSurfaceNormals(vertices, shape, profiledSamples);
                ushort[] indices = BuildTbindGridIndices(rowCount, columnCount);
                return new EffectShapeMeshData(vertices, indices, PrimitiveType.TriangleStrip, Math.Max(0, indices.Length - 2));
            }

            return new EffectShapeMeshData(vertices, null, PrimitiveType.TriangleStrip, Math.Max(0, vertices.Length - 2));
        }

        private static EffectShapeMeshData CreateEmptyTbindMesh()
        {
            return new EffectShapeMeshData(new VertexPositionNormalColorTexture[0], null, PrimitiveType.TriangleStrip, 0);
        }

        private static SimdVector3 GetTbindVertexNormal(EffectShapeSegment row, TbindRowFrame[] rowFrames, int rowIndex, bool autoOrientation)
        {
            if (autoOrientation && rowFrames != null)
                return rowFrames[rowIndex].Normal;

            SimdVector3 normal = GetTransformDirection(row.Transform, SimdVector3.UnitZ);
            return TryNormalize(normal, out normal) ? normal : SimdVector3.UnitZ;
        }

        private static SimdVector3 GetTbindVertexTangent(EffectShapeSegment row, TbindRowFrame[] rowFrames, int rowIndex, bool autoOrientation)
        {
            if (autoOrientation && rowFrames != null)
                return rowFrames[rowIndex].WidthAxis;

            SimdVector3 tangent = GetTransformDirection(row.Transform, SimdVector3.UnitX);
            return TryNormalize(tangent, out tangent) ? tangent : SimdVector3.UnitX;
        }

        private static VertexPositionNormalColorTexture CreateTrailVertex(SimdVector3 position, SimdVector3 normal, SimdVector3 tangent, Color color, float u, float v)
        {
            return new VertexPositionNormalColorTexture(
                new Vector3(position.X, position.Y, position.Z),
                new Vector3(normal.X, normal.Y, normal.Z),
                new Vector3(tangent.X, tangent.Y, tangent.Z),
                new Vector2(u, v),
                color);
        }

        private static ushort[] BuildTbindGridIndices(int rowCount, int columnCount)
        {
            ushort[] indices = new ushort[(rowCount - 1) * columnCount * 2];
            int index = 0;

            for (int rowIndex = 0; rowIndex < rowCount - 1; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    indices[index++] = (ushort)((rowIndex * columnCount) + columnIndex);
                    indices[index++] = (ushort)(((rowIndex + 1) * columnCount) + columnIndex);
                }
            }

            return indices;
        }

        private static Color GetTbindColumnColor(EffectShapeSegment segment, int columnIndex, int columnCount)
        {
            if (columnCount == 2 && columnIndex == 1)
                return ApplyAlphaScale(segment.SecondaryColor, segment.AlphaScale);

            return ApplyAlphaScale(segment.PrimaryColor, segment.AlphaScale);
        }

        private static Vector2 GetTbindGridUv(IList<EffectShapePoint> shape, EffectShapeSegment firstRow, int rowIndex, int rowCount, int columnIndex, int columnCount, float scrollU, float scrollV, float stepU, float stepV)
        {
            if (columnCount == 2)
            {
                float baseU = SafeDivide(columnIndex, columnCount - 1);
                float baseV = SafeDivide(rowIndex, rowCount - 1);
                return new Vector2(scrollU + (stepU * baseU), scrollV + (stepV * baseV));
            }

            float u = firstRow.UvBaseU + (1f - shape[columnIndex].Y) * 0.5f;
            float v = firstRow.UvBaseV + SafeDivide(rowIndex, rowCount - 1);
            return new Vector2(scrollU + (stepU * u), scrollV + (stepV * v));
        }

        public static List<EffectShapePoint> CreateDefaultRibbonShape()
        {
            return new List<EffectShapePoint>
            {
                new EffectShapePoint(-0.5f, 0f),
                new EffectShapePoint(0.5f, 0f)
            };
        }
    }
}
