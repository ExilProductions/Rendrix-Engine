using System;
using System.Collections.Generic;
using System.Numerics;

namespace PhysNet.Collision.Broadphase
{
    internal class DynamicAabbTree<T>
    {
        private struct Node
        {
            public Aabb Box;
            public int Parent;
            public int Left;
            public int Right;
            public int Height;
            public T Item;
            public bool IsLeaf => Left == -1 && Right == -1;
        }

        private int _root = -1;
        private readonly List<Node> _nodes = new();
        private readonly Stack<int> _free = new();

        private int Allocate()
        {
            if (_free.Count > 0)
                return _free.Pop();
            _nodes.Add(default);
            return _nodes.Count - 1;
        }

        private void Free(int index)
        {
            _nodes[index] = default;
            _free.Push(index);
        }

        public int Insert(Aabb box, T item)
        {
            int id = Allocate();
            _nodes[id] = new Node
            {
                Box = box,
                Parent = -1,
                Left = -1,
                Right = -1,
                Height = 0,
                Item = item
            };
            InsertLeaf(id);
            return id;
        }

        public void Remove(int id)
        {
            if (id == _root)
            {
                _root = -1;
                Free(id);
                return;
            }

            int parent = _nodes[id].Parent;
            int grandParent = _nodes[parent].Parent;
            int sibling = _nodes[parent].Left == id ? _nodes[parent].Right : _nodes[parent].Left;

            if (grandParent != -1)
            {
                var gp = _nodes[grandParent];
                if (gp.Left == parent) gp.Left = sibling; else gp.Right = sibling;
                _nodes[grandParent] = gp;

                var s = _nodes[sibling];
                s.Parent = grandParent;
                _nodes[sibling] = s;

                FixUpwardsTree(grandParent);
            }
            else
            {
                _root = sibling;
                var s = _nodes[sibling];
                s.Parent = -1;
                _nodes[sibling] = s;
            }

            Free(parent);
            Free(id);
        }

        public void Update(int id, Aabb box)
        {
            Remove(id);
            Insert(box, _nodes[id].Item);
        }

        private void InsertLeaf(int leaf)
        {
            if (_root == -1)
            {
                _root = leaf;
                var r = _nodes[_root];
                r.Parent = -1;
                _nodes[_root] = r;
                return;
            }

            int index = _root;
            var leafBox = _nodes[leaf].Box;
            while (!_nodes[index].IsLeaf)
            {
                int left = _nodes[index].Left;
                int right = _nodes[index].Right;

                float area = _nodes[index].Box.SurfaceArea();
                var combined = _nodes[index].Box;
                combined.Encapsulate(leafBox);
                float combinedArea = combined.SurfaceArea();

                //cost if creating a new parent for this node and the new leaf
                float cost = 2f * combinedArea;

                //minimum cost of pushing the leaf down the tree
                float inheritanceCost = 2f * (combinedArea - area);

                float costLeft = left == -1 ? float.PositiveInfinity :
                    (_nodes[left].IsLeaf
                        ? _nodes[left].Box.EncapsulatedSurfaceArea(leafBox) + inheritanceCost
                        : (_nodes[left].Box.EncapsulatedSurfaceArea(leafBox) - _nodes[left].Box.SurfaceArea()) + inheritanceCost);

                float costRight = right == -1 ? float.PositiveInfinity :
                    (_nodes[right].IsLeaf
                        ? _nodes[right].Box.EncapsulatedSurfaceArea(leafBox) + inheritanceCost
                        : (_nodes[right].Box.EncapsulatedSurfaceArea(leafBox) - _nodes[right].Box.SurfaceArea()) + inheritanceCost);

                if (cost < costLeft && cost < costRight)
                    break;

                index = costLeft < costRight ? left : right;
            }

            int siblingIndex = index;
            int oldParent = _nodes[siblingIndex].Parent;
            int newParent = Allocate();
            _nodes[newParent] = new Node
            {
                Parent = oldParent,
                Left = siblingIndex,
                Right = leaf,
                Height = _nodes[siblingIndex].Height + 1,
                Box = Combine(_nodes[siblingIndex].Box, _nodes[leaf].Box)
            };

            var sib = _nodes[siblingIndex]; sib.Parent = newParent; _nodes[siblingIndex] = sib;
            var lf = _nodes[leaf]; lf.Parent = newParent; _nodes[leaf] = lf;

            if (oldParent != -1)
            {
                var op = _nodes[oldParent];
                if (op.Left == siblingIndex) op.Left = newParent; else op.Right = newParent;
                _nodes[oldParent] = op;
            }
            else
            {
                _root = newParent;
            }

            FixUpwardsTree(newParent);
        }

        private static Aabb Combine(in Aabb a, in Aabb b)
        {
            return new Aabb(Vector3.Min(a.Min, b.Min), Vector3.Max(a.Max, b.Max));
        }

        private void FixUpwardsTree(int index)
        {
            while (index != -1)
            {
                int left = _nodes[index].Left;
                int right = _nodes[index].Right;

                if (left == -1 || right == -1)
                {
                    var n0 = _nodes[index];
                    n0.Height = 0;
                    _nodes[index] = n0;
                    index = _nodes[index].Parent;
                    continue;
                }

                var n = _nodes[index];
                n.Height = 1 + System.Math.Max(_nodes[left].Height, _nodes[right].Height);
                n.Box = Combine(_nodes[left].Box, _nodes[right].Box);
                _nodes[index] = n;

                index = _nodes[index].Parent;
            }
        }

        public void Query(Aabb box, List<int> results)
        {
            if (_root == -1) return;
            var stack = new Stack<int>();
            stack.Push(_root);
            while (stack.Count > 0)
            {
                int id = stack.Pop();
                var n = _nodes[id];
                if (!n.Box.Overlaps(box)) continue;
                if (n.IsLeaf) results.Add(id);
                else { if (n.Left != -1) stack.Push(n.Left); if (n.Right != -1) stack.Push(n.Right); }
            }
        }

        public bool RayCast(Vector3 origin, Vector3 dir, float maxDist, Func<T, Aabb, bool> callback)
        {
            if (_root == -1) return false;
            var d = dir;
            var stack = new Stack<int>();
            stack.Push(_root);
            while (stack.Count > 0)
            {
                int id = stack.Pop();
                var n = _nodes[id];
                if (!RayAabb(origin, d, maxDist, n.Box)) continue;
                if (n.IsLeaf)
                {
                    if (!callback(n.Item, n.Box)) return false;
                }
                else { if (n.Left != -1) stack.Push(n.Left); if (n.Right != -1) stack.Push(n.Right); }
            }
            return true;
        }

        private static bool RayAabb(Vector3 o, Vector3 d, float max, in Aabb b)
        {
            float tmin = 0f;
            float tmax = max;

            //X slab
            if (System.MathF.Abs(d.X) < 1e-8f)
            {
                if (o.X < b.Min.X || o.X > b.Max.X) return false;
            }
            else
            {
                float inv = 1f / d.X;
                float t1 = (b.Min.X - o.X) * inv;
                float t2 = (b.Max.X - o.X) * inv;
                if (t1 > t2) (t1, t2) = (t2, t1);
                tmin = System.MathF.Max(tmin, t1);
                tmax = System.MathF.Min(tmax, t2);
                if (tmin > tmax) return false;
            }

            //Y slab
            if (System.MathF.Abs(d.Y) < 1e-8f)
            {
                if (o.Y < b.Min.Y || o.Y > b.Max.Y) return false;
            }
            else
            {
                float inv = 1f / d.Y;
                float t1 = (b.Min.Y - o.Y) * inv;
                float t2 = (b.Max.Y - o.Y) * inv;
                if (t1 > t2) (t1, t2) = (t2, t1);
                tmin = System.MathF.Max(tmin, t1);
                tmax = System.MathF.Min(tmax, t2);
                if (tmin > tmax) return false;
            }

            //Z slab
            if (System.MathF.Abs(d.Z) < 1e-8f)
            {
                if (o.Z < b.Min.Z || o.Z > b.Max.Z) return false;
            }
            else
            {
                float inv = 1f / d.Z;
                float t1 = (b.Min.Z - o.Z) * inv;
                float t2 = (b.Max.Z - o.Z) * inv;
                if (t1 > t2) (t1, t2) = (t2, t1);
                tmin = System.MathF.Max(tmin, t1);
                tmax = System.MathF.Min(tmax, t2);
                if (tmin > tmax) return false;
            }

            return tmax >= 0f && tmin <= max;
        }

        public (Aabb box, T item) GetLeaf(int id)
        {
            var n = _nodes[id];
            return (n.Box, n.Item);
        }

        public void SetLeafBox(int id, Aabb box)
        {
            var n = _nodes[id];
            n.Box = box;
            _nodes[id] = n;
        }
    }
}
