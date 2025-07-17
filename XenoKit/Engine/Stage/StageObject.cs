using System.Collections.Generic;
using XenoKit.Engine.Model;
using Xv2CoreLib.FMP;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Stage
{
    public class StageObject
    {
        public FMP_Object Object { get; set; }
        public Matrix4x4 Transform { get; set; }
        public List<StageEntity> Entities { get; set; } = new List<StageEntity>();
        public List<StageColliderInstance> ColliderInstances { get; set; } = new List<StageColliderInstance>();

        public void Draw()
        {
            if (SceneManager.StageGeometryVisible)
            {
                foreach (var entity in Entities)
                {
                    entity.Draw(Transform);
                }
            }

            /*
            if (SceneManager.CollisionMeshVisible)
            {
                foreach(var collider in ColliderInstances)
                {
                    collider.Draw(Transform, true);
                }
            }
            */
        }

        public void DrawSimple()
        {
            foreach (var entity in Entities)
            {
                entity.DrawSimple(Transform);
            }
        }

        public void SetColliderMeshWorld()
        {
            foreach(var collider in ColliderInstances)
            {
                collider.SetColliderMeshWorld(Transform);
            }
        }

        public List<CollisionMesh> GetAllCollisionMeshes()
        {
            List<CollisionMesh> meshes = new List<CollisionMesh>();

            foreach(var collider in ColliderInstances)
            {
                meshes.AddRange(collider.GetAllCollisionMeshes());
            }

            return meshes;
        }

        public override string ToString()
        {
            return Object.Name;
        }
    }
}
