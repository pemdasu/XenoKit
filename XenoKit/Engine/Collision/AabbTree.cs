using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using XenoKit.Engine.Objects;

namespace XenoKit.Engine.Collision
{
    public class AabbTree<T> where T : IBvhNode
    {
        private readonly T[] Leafs;
        private readonly AabbNode[] Nodes;
        private readonly int RootIndex;
        private readonly int[] TraversalStack;
        private int StackIdx;
        private readonly DrawableBoundingBox debugCube;

        public const int MAX_HIT_RESULTS = 24;
        private readonly List<T> _hitResults = new List<T>(MAX_HIT_RESULTS);
        private readonly List<T> _hitResultsTemp = new List<T>(MAX_HIT_RESULTS);
        private readonly List<float> _hitDistances = new List<float> (MAX_HIT_RESULTS);
        private readonly List<int> _sortedDistanceList = new List<int>(MAX_HIT_RESULTS);

        #region Creation
        public AabbTree(IList<T> source)
        {
            Leafs = source.ToArray();
            List<AabbNode> nodes = GroupSourceNodes(Leafs);

            HashSet<int> alreadyGrouped = new HashSet<int>(Leafs.Length * 4);
            List<int> parents = new List<int>(Leafs.Length * 4);

            for (int i = 0; i < nodes.Count; i++)
            {
                alreadyGrouped.Add(i);
                parents.Add(-1);
            }

            while(GetNumWithNoParent(parents) > 1)
            {
                GroupNodes(nodes, parents, alreadyGrouped);
            }

            Nodes = nodes.ToArray();
            RootIndex = Nodes.Length - 1;
            TraversalStack = new int[Nodes.Length];

#if DEBUG
            debugCube = new DrawableBoundingBox();
#endif
        }

        private List<AabbNode> GroupSourceNodes(T[] sourceNodes)
        {
            List<AabbNode> groupedNodes = new List<AabbNode>();
            HashSet<int> alreadyGrouped = new HashSet<int>();

            for(int i = 0; i < sourceNodes.Count(); i++)
            {
                if (alreadyGrouped.Contains(i)) continue;

                AabbNode aabbNode = new AabbNode
                {
                    LeftAABB = sourceNodes[i].GetAABB(),
                    LeftIndex = -(i + 1)
                };
                alreadyGrouped.Add(i);

                int closestIdx = FindClosestNode(sourceNodes, i, alreadyGrouped);

                if (closestIdx != -1)
                {
                    aabbNode.RightAABB = sourceNodes[closestIdx].GetAABB();
                    aabbNode.RightIndex = -(closestIdx + 1);
                    alreadyGrouped.Add(closestIdx);
                }

                groupedNodes.Add(aabbNode);
            }

            return groupedNodes;
        }

        private void GroupNodes(List<AabbNode> nodes, List<int> parents, HashSet<int> alreadyGrouped)
        {
            int initialCount = nodes.Count;

            for (int i = 0; i < initialCount; i++)
            {
                if(!alreadyGrouped.Contains(i)) continue;
                int closest = FindClosestNode(nodes, i, alreadyGrouped);

                AabbNode newNode = new AabbNode()
                {
                    LeftAABB = nodes[i].GetAABB(),
                    LeftIndex = i + 1
                };
                parents[i] = nodes.Count;
                alreadyGrouped.Add(i);

                if(closest != -1)
                {
                    newNode.RightAABB = nodes[closest].GetAABB();
                    newNode.RightIndex = closest + 1;
                    parents[closest] = nodes.Count;
                    alreadyGrouped.Add(closest);
                }

                nodes.Add(newNode);
                parents.Add(-1);
            }
        }

        private static int FindClosestNode<N>(IList<N> nodes, int mainIdx, HashSet<int> alreadyGrouped) where N : IBvhNode
        {
            Vector3 mainCenter = nodes[mainIdx].GetAABBCenter();
            int lowest = -1;
            float lowestDist = float.PositiveInfinity;

            for (int i = 0; i < nodes.Count; i++)
            {
                //Skip calculating distance for nodes that have already been grouped
                if (!alreadyGrouped.Contains(i) && i != mainIdx)
                {
                    Vector3 center = nodes[i].GetAABBCenter();
                    float distance = Vector3.Distance(center, mainCenter);

                    if(distance < lowestDist)
                    {
                        lowest = i;
                        lowestDist = distance;
                    }
                }
            }

            return lowest;
        }
    
        private static int GetNumWithNoParent(IList<int> parentList)
        {
            int num = 0;

            for(int i = 0; i < parentList.Count; i++)
            {
                if (parentList[i] == -1)
                    num++;
            }

            return num;
        }
        #endregion

        #region Traversal
        /// <summary>
        /// Tests the BVH against a ray for any hit at all.
        /// </summary>
        /// <returns>true if the ray intersects the BVH at all, otherwise, false</returns>
        public bool IntersectsAny(Ray ray)
        {
            StackIdx = 0;
            TraversalStack[StackIdx++] = RootIndex;

            while (StackIdx > 0)
            {
                int index = TraversalStack[--StackIdx];

                Nodes[index].LeftAABB.Intersects(ref ray, out float? leftResult);

                if (leftResult.HasValue)
                {
                    //Leafs are stored as negative numbers
                    if (Nodes[index].LeftIndex < 0)
                    {
                        return true;
                    }
                    else
                    {
                        TraversalStack[StackIdx++] = Nodes[index].LeftIndex - 1;
                    }
                }

                Nodes[index].RightAABB.Intersects(ref ray, out float? rightResult);

                if (rightResult.HasValue)
                {
                    if (Nodes[index].RightIndex < 0)
                    {
                        return true;
                    }
                    else
                    {
                        TraversalStack[StackIdx++] = Nodes[index].RightIndex - 1;
                    }
                }


            }

            return false;
        }

        public bool Intersects(Ray ray, out T result)
        {
            StackIdx = 0;
            TraversalStack[StackIdx++] = RootIndex;

            float closestIntersection = float.PositiveInfinity;
            T hit = default;

            while (StackIdx > 0)
            {
                int index = TraversalStack[--StackIdx];

                Nodes[index].LeftAABB.Intersects(ref ray, out float? leftResult);

                if (leftResult.HasValue)
                {
                    //Leafs are stored as negative numbers
                    if(Nodes[index].LeftIndex < 0)
                    {
                        if(leftResult.Value < closestIntersection)
                        {
                            closestIntersection = leftResult.Value;
                            hit = Leafs[-(Nodes[index].LeftIndex + 1)];
                        }
                    }
                    else
                    {
                        TraversalStack[StackIdx++] = Nodes[index].LeftIndex - 1;
                    }
                }

                Nodes[index].RightAABB.Intersects(ref ray, out float? rightResult);

                if (rightResult.HasValue)
                {
                    if (Nodes[index].RightIndex < 0)
                    {
                        if (rightResult.Value < closestIntersection)
                        {
                            closestIntersection = rightResult.Value;
                            hit = Leafs[-(Nodes[index].RightIndex + 1)];
                        }
                    }
                    else
                    {
                        TraversalStack[StackIdx++] = Nodes[index].RightIndex - 1;
                    }
                }


            }

            if(closestIntersection != float.PositiveInfinity)
            {
                result = hit;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Tests the BVH against a ray, and returns an array containing all objects that are hit (up to a maximum amount specified by <see cref="MAX_HIT_RESULTS"/>), sorted by distance
        /// </summary>
        public IList<T> Intersects(Ray ray)
        {
            StackIdx = 0;
            TraversalStack[StackIdx++] = RootIndex;

            float closestIntersection = float.PositiveInfinity;
            _hitResults.Clear();
            _hitDistances.Clear();

            while (StackIdx > 0)
            {
                int index = TraversalStack[--StackIdx];

                Nodes[index].LeftAABB.Intersects(ref ray, out float? leftResult);

                if (leftResult.HasValue)
                {
                    //Leafs are stored as negative numbers
                    if (Nodes[index].LeftIndex < 0)
                    {
                        if (_hitResults.Count < MAX_HIT_RESULTS)
                        {
                            _hitResults.Add(Leafs[-(Nodes[index].LeftIndex + 1)]);
                            _hitDistances.Add(leftResult.Value);
                        }
                        else if(leftResult.Value < closestIntersection)
                        {
                            //Replace furthest hit with the closer intersection
                            int idx = GetIndexOfFurthestHit();
                            _hitResults[idx] = Leafs[-(Nodes[index].LeftIndex + 1)];
                            _hitDistances[idx] = leftResult.Value;
                        }

                        if(leftResult.Value < closestIntersection)
                            closestIntersection = leftResult.Value;
                    }
                    else
                    {
                        TraversalStack[StackIdx++] = Nodes[index].LeftIndex - 1;
                    }
                }

                Nodes[index].RightAABB.Intersects(ref ray, out float? rightResult);

                if (rightResult.HasValue)
                {
                    if (Nodes[index].RightIndex < 0)
                    {
                        if (_hitResults.Count < MAX_HIT_RESULTS)
                        {
                            _hitResults.Add(Leafs[-(Nodes[index].RightIndex + 1)]);
                            _hitDistances.Add(rightResult.Value);
                        }
                        else if (rightResult.Value < closestIntersection)
                        {
                            //Replace furthest hit with the closer intersection
                            int idx = GetIndexOfFurthestHit();
                            _hitResults[idx] = Leafs[-(Nodes[index].RightIndex + 1)];
                            _hitDistances[idx] = rightResult.Value;
                        }

                        if (rightResult.Value < closestIntersection)
                            closestIntersection = rightResult.Value;
                    }
                    else
                    {
                        TraversalStack[StackIdx++] = Nodes[index].RightIndex - 1;
                    }
                }


            }

            //Sort results based on distance
            _sortedDistanceList.Clear();
            for (int i = 0; i < _hitDistances.Count; i++)
                _sortedDistanceList.Add(i);

            for (int i = 0; i < _hitDistances.Count - 1; i++)
            {
                _sortedDistanceList.Add(i);

                for (int j = i + 1; j < _hitDistances.Count; j++)
                {
                    if (_hitDistances[_sortedDistanceList[j]] < _hitDistances[_sortedDistanceList[i]])
                    {
                        int temp = _sortedDistanceList[i];
                        _sortedDistanceList[i] = _sortedDistanceList[j];
                        _sortedDistanceList[j] = temp;
                    }
                }
            }

            _hitResultsTemp.Clear();
            for (int i = 0; i < _sortedDistanceList.Count; i++)
            {
                _hitResultsTemp.Add(_hitResults[_sortedDistanceList[i]]);
            }

            /*
            _sortedDistanceList.Clear();
            _hitResultsTemp.Clear();

            for(int i = 0; i < _hitDistances.Count; i++)
            {
                int idx = 0;

                for(int a = 0; a < _hitDistances.Count; a++)
                {
                    if (_hitDistances[a] < _hitDistances[i])
                        idx++;
                }

                _sortedDistanceList.Add(idx);
            }

            for(int i = 0; i < _hitResults.Count; i++)
            {
                _hitResultsTemp.Add(_hitResults[_sortedDistanceList[i]]);
            }
            */
            return _hitResultsTemp;
        }

        private int GetIndexOfFurthestHit()
        {
            float furthest = float.NegativeInfinity;
            int index = 0;

            for(int i = 0; i <  _hitDistances.Count; i++)
            {
                if (_hitDistances[i] > furthest)
                {
                    index = i;
                    furthest = _hitDistances[i];
                }
            }

            return index;
        }
        #endregion

        public void DebugDraw(System.Numerics.Matrix4x4 world)
        {
            if (debugCube == null) return;

            for(int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].LeftIndex != 0)
                {
                    debugCube.SetBounds(Nodes[i].LeftAABB);
                    debugCube.Draw(world);
                }

                if (Nodes[i].RightIndex != 0)
                {
                    debugCube.SetBounds(Nodes[i].RightAABB);
                    debugCube.Draw(world);
                }
            }
        }
    }

    public interface IBvhNode
    {
        BoundingBox GetAABB();
        Vector3 GetAABBCenter();
    }

}