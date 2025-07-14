using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XenoKit.Engine.Collision
{
    public class AabbTree<T> where T : IAabbTreeNode
    {
        private T[] Leafs;
        private AabbNode[] Nodes;

        public AabbTree(IEnumerable<T> source)
        {
            Leafs = source.ToArray();
            List<AabbNode> nodes = new List<AabbNode>();

            for(int i = 0; i < Leafs.Length; i++)
            {
            }
        }

        private static void CalculateDistances(List<DistanceSort> distances, List<T> nodes, T node, HashSet<int> alreadyGrouped)
        {
            distances.Clear();

            for (int i = 0; i < nodes.Count; i++)
            {
                //Skip calculating distance for nodes that have already been grouped
                if (!alreadyGrouped.Contains(i))
                {
                    distances.Add(new DistanceSort()
                    {
                        Distance = Vector3.Distance(nodes[i].BoundingBoxCenter, node.BoundingBoxCenter),
                        Index = i
                    });
                }
            }
        }

        private struct DistanceSort
        {
            internal int Index;
            internal float Distance;
        }
    }

    public interface IAabbTreeNode
    {
        Matrix AttachMatrix { get; }
        BoundingBox BoundingBox { get; }
        Vector3 BoundingBoxCenter { get; }
    }

}