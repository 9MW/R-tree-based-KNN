
#define LOG
using System;
using System.Collections;
using System.Collections.Generic;
using Extensions;
using System.Linq;
using Priority_Queue;

namespace RTREE
{

    public class RBush<U> where U: FastPriorityQueueNode,spatialAttribute
    {

        double intersectionArea(node<U> a, node<U> b)
        {
            double minX = Math.Max(a.minX, b.minX),
                minY = Math.Max(a.minY, b.minY),
                maxX = Math.Min(a.maxX, b.maxX),
                maxY = Math.Min(a.maxY, b.maxY);

            return Math.Max(0, maxX - minX) *
                   Math.Max(0, maxY - minY);
        }
        private RBush(Func<node<U>, node<U>> toBBox)
        {
            this.toBBox = toBBox;
            compareMinX = compareNodeMinX;
            compareMinY = compareNodeMinY;
            _minEntries = Math.Max(2,(int) Math.Ceiling((_maxEntries * 0.4)));
        }

        public RBush(IComparer<U> compareX, IComparer<U> compareY, Func<node<U>, node<U>> toBBox) : this(toBBox)
        {
            this.compareX = compareX;
            this.compareY = compareY;
        }
        node<U> extend(node<U> a, node<U> b)
        {
            a.minX = Math.Min(a.minX, b.minX);
            a.minY = Math.Min(a.minY, b.minY);
            a.maxX = Math.Max(a.maxX, b.maxX);
            a.maxY = Math.Max(a.maxY, b.maxY);
            return a;
        }
        Comparison<node<U>> compareNodeMinX = (a, b) =>
        {
            if (a.minX > b.minX)
                return 1;
            if (a.minX < b.minX)
                return -1;
            return 0;
        };
        Comparison<node<U>> compareNodeMinY = (a, b) =>
        {
            if (a.minY > b.minY)
                return 1;
            if (a.minY < b.minY)
                return -1;
            return 0;
        };
        Func<node<U>, double> bboxArea = (a) => { return (a.maxX - a.minX) * (a.maxY - a.minY); };
        Func<node<U>, double> bboxMargin = (a) => { return (a.maxX - a.minX) + (a.maxY - a.minY); };
        Func<node<U>, node<U>, double> enlargedArea = (a, b) =>
        {
            return (Math.Max(b.maxX, a.maxX) - Math.Min(b.minX, a.minX)) *
               (Math.Max(b.maxY, a.maxY) - Math.Min(b.minY, a.minY));
        };
        IComparer<U> compareX, compareY;
        Comparison<node<U>> compareMinX, compareMinY;
        Func<node<U>, node<U>, bool> intersects = (a, b) =>
        {
            return b.minX <= a.maxX &&
               b.minY <= a.maxY &&
               b.maxX >= a.minX &&
               b.maxY >= a.minY;
        };
        Func<node<U>, node<U>> toBBox; 
            //= (a) => {

            //for (int i = 0; i < a.elements.Count; i++)
            //{
            //    T ele = a.elements[i];
            //    a.maxX = Math.Max(a.maxX, ele.x);
            //    a.minX = Math.Min(a.minX, ele.x);
            //    a.minY = Math.Min(a.minY, ele.y);
            //    a.maxY = Math.Max(a.maxY, ele.y);
            //}
            //return a; };
        // min bounding rectangle of node children from k to p-1
        node<U> distBBox(node<U> node, int k, int p, Func<node<U>, node<U>> toBBox, node<U> destNode = null)
        {
            if (!destNode) destNode = createNode(null);
            destNode.minX =  double.PositiveInfinity;
            destNode.minY =  double.PositiveInfinity;
            destNode.maxX =  double.NegativeInfinity;
            destNode.maxY =  double.NegativeInfinity;
            node<U> child;
            for (var i = k; i < p; i++)
            {
                child = node.children[i];
                if (node.leaf) toBBox(child);else extend(destNode,child);
                //extend(destNode, node.leaf ? toBBox(child) : child);
            }

            return destNode;
        }
        node<U> _chooseSubtree(node<U> bbox, node<U> node, int level, List<node<U>> path)
        {

            int i, len;
            double minArea, minEnlargement, area, enlargement;
            node<U> child, targetNode = null;
            while (true)
            {
                path.Add(node);

                if (node.leaf || path.Count - 1 == level) break;

                minArea = minEnlargement = double.PositiveInfinity;

                for (i = 0, len = node.children.Count; i < len; i++)
                {
                    child = node.children[i];
                    area = bboxArea(child);
                    enlargement = enlargedArea(bbox, child) - area;

                    // choose entry with the least area enlargement
                    if (enlargement < minEnlargement)
                    {
                        minEnlargement = enlargement;
                        minArea = area < minArea ? area : minArea;
                        targetNode = child;

                    }
                    else if (enlargement == minEnlargement)
                    {
                        // otherwise choose one with the smallest area
                        if (area < minArea)
                        {
                            minArea = area;
                            targetNode = child;
                        }
                    }
                }

                node = targetNode != null ? targetNode : node.children[0];
            }

            return node;
        }
        int _minEntries = 9, _maxEntries = 9;
        void _split(List<node<U>> insertPath, int level)
        {

            var node = insertPath[level];
            int M = node.children.Count,
              m = this._minEntries;

            this._chooseSplitAxis(node, m, M);

            var splitIndex = this._chooseSplitIndex(node, m, M);

            var newNode = createNode(node.children.Splice(splitIndex, node.children.Count - splitIndex));
            newNode.height = node.height;
            newNode.leaf = node.leaf;

            calcBBox(node, this.toBBox);
            calcBBox(newNode, this.toBBox);

            if (level != 0) insertPath[level - 1].children.Add(newNode);
            else this._splitRoot(node, newNode);
        }
       public  node<U> data;
        void _splitRoot(node<U> node, node<U> newNode)
        {
            // split root node
            this.data = createNode(new List<node<U>>() { node, newNode });
            this.data.height = node.height + 1;
            this.data.leaf = false;
            calcBBox(this.data, this.toBBox);
        }
        void calcBBox(node<U> node, Func<node<U>, node<U>> toBBox)
        {
            if (node.leaf)
            {
                    toBBox(node);
                return;
            }
             distBBox(node, 0, node.children.Count, toBBox, node);
        }
        void _chooseSplitAxis(node<U> node, int m, int M)
        {

            Comparison<node<U>> compareMinX = node.leaf ? this.compareMinX : compareNodeMinX,
                compareMinY = node.leaf ? this.compareMinY : compareNodeMinY;
            double xMargin = this._allDistMargin(node, m, M, compareMinX),
               yMargin = this._allDistMargin(node, m, M, compareMinY);

            // if total distributions margin value is minimal for x, sort by minX,
            // otherwise it's already sorted by minY
            if (xMargin < yMargin) node.children.Sort(compareMinX);
        }
        double _allDistMargin(node<U> node, int m, int M, Comparison<node<U>> compare)
        {

            node.children.Sort(compare);
            node<U> leftBBox = distBBox(node, 0, m, toBBox),
               rightBBox = distBBox(node, M - m, M, toBBox), child;
            double margin = bboxMargin(leftBBox) + bboxMargin(rightBBox);
            int i;

            for (i = m; i < M - m; i++)
            {
                child = node.children[i];
                extend(leftBBox, node.leaf ? toBBox(child) : child);
                margin += bboxMargin(leftBBox);
            }

            for (i = M - m - 1; i >= m; i--)
            {
                child = node.children[i];
                extend(rightBBox, node.leaf ? toBBox(child) : child);
                margin += bboxMargin(rightBBox);
            }

            return margin;
        }
        int _chooseSplitIndex(node<U> node, int m, int M)
        {

            node<U> bbox1, bbox2; int i, index = -1;
            double overlap, area, minOverlap, minArea;
            minOverlap = minArea = double.PositiveInfinity;

            for (i = m; i <= M - m; i++)
            {
                bbox1 = distBBox(node, 0, i, this.toBBox);
                bbox2 = distBBox(node, i, M, this.toBBox);

                overlap = intersectionArea(bbox1, bbox2);
                area = bboxArea(bbox1) + bboxArea(bbox2);

                // choose distribution with minimum overlap
                if (overlap < minOverlap)
                {
                    minOverlap = overlap;
                    index = i;

                    minArea = area < minArea ? area : minArea;

                }
                else if (overlap == minOverlap)
                {
                    // otherwise choose distribution with minimum area
                    if (area < minArea)
                    {
                        minArea = area;
                        index = i;
                    }
                }
            }

            return index;
        }
        public RBush<U> load (U[] data)
        {
            if (data.Length==0&&this.data==null) return this;

            if (data.Length < this._minEntries)
            {
                for (int i = 0, len = data.Length; i < len; i++)
                {
                    _insert(new node<U>(data[i]), this.data.height - 1, false);
                }
                return this;
            }

            // recursively build the tree with the given data from scratch using OMT algorithm
            var node = BuildNodes(data.ToList(), 0, data.Length - 1, 0);
#if LOG
            print("new node time=" + sw.Elapsed.TotalMilliseconds + "ms");
#endif
            if (this.data==null||this.data.children.Count==0)
            {
                // save as is if tree is empty
                this.data = node;

            }
            else if (this.data.height == node.height)
            {
                // split root if trees have the same height
                this._splitRoot(this.data, node);

            }
            else
            {
                if (this.data.height < node.height)
                {
                    // swap trees if inserted one is bigger
                    var tmpNode = this.data;
                    this.data = node;
                    node = tmpNode;
                }

                // insert the small tree into the large tree at appropriate level
                this._insert(node, this.data.height - node.height - 1, true);
            }

            return this;
        }

        private void print(string v)
        {
            //custom by you self
            Console.Write(v);
        }

        node<U> distBBox(node<U> node, int k, int p, node<U> toBBox, node<U> destNode)
        {
            if (!destNode) destNode = createNode(null);
            destNode.minX = double.PositiveInfinity;
            destNode.minY =  double.PositiveInfinity;
            destNode.maxX =  double.NegativeInfinity;
            destNode.maxY =  double.NegativeInfinity;
            node<U> child;
            for (var i = k; i < p; i++)
            {
                child = node.children[i];
                extend(destNode, child);
            }

            return destNode;
        }
        node<U> createNode(List<node<U>> children)
        {
            return new node<U>(children);
        }
        void _insert(node<U> item,int level,bool isNode) {
            var toBBox = this.toBBox;
            node<U> bbox =item;
                      List< node < U >> insertPath=new List<node<U>>() ;

            // find the best node for accommodating the item, saving all nodes along the path too
            var node = this._chooseSubtree(bbox, this.data, level, insertPath);

            // put the item into the node
            node.children.Add(item);
            extend(node, bbox);

            // split on node overflow; propagate upwards if necessary
            while (level >= 0)
            {
                if (insertPath[level].children.Count > this._maxEntries)
                {
                    this._split(insertPath, level);
                    level--;
                }
                else break;
            }

            // adjust bboxes along the insertion path
            this._adjustParentBBoxes(bbox, insertPath, level);
        }
        //void TreeTest<M>(RBTree<M> tree)where M: FastPriorityQueueNode
        //{
        //    Stack<node<M>> np = new Stack<node<M>>();
        //    np.Push(tree.data);
        //    List<M> lp = new List<M>(3000);
        //    while (np.Count > 0)
        //    {
        //        var j = np.Pop();
        //        if (j.leaf)
        //        {
        //            for (int i = 0; i < j.elements.Count; i++)
        //            {
        //                if (lp.Contains(j.elements[i]))
        //                {
        //                    Debug.LogError("same elements in tree");
        //                }
        //                else
        //                {
        //                    lp.Add(j.elements[i]);
        //                }
        //            }
        //            continue;
        //        }
        //        for (int i = 0; i < j.children.Count; i++)
        //        {
        //            np.Push(j.children[i]);
        //        }
        //    }
        //    Debug.Log("R-tree correct");
        //}
        void _adjustParentBBoxes(node<U> bbox, List<node<U>> path, int level)
        {
            // adjust bboxes along the given tree path
            for (var i = level; i >= 0; i--)
            {
                extend(path[i], bbox);
            }
        }
        void multiSelect(List<U> arr,int left,int right,int n,IComparer<U> compare)
        {
            var stack = new List<int>() { left, right };
            int    mid;
            bool kk = false;
            while (stack.Count>0)
            {
                right = stack.pop();
                left = stack.pop();

                if (right - left <= n) continue;
                float veil = (float)(right- left ) / (n * 2) * 1.0f;
                int ceil =(int) Math.Ceiling(veil);
                mid = left + ceil * n;
                sw.Start();
                quickselect(arr, mid, left, right, compare);
                sw.Stop();
                stack.Add(left);
                stack.Add(mid);
                stack.Add(mid);
                stack.Add(right);
                //stack.AddRange(new []{ left, mid, mid, right});
            }
            if (kk) throw new Exception();
        }
       void swap(List<U> arr,int i,int j)
        {
            var tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }

        void quickselect(List<U> arr,int k, int left, int right,IComparer<U> compare)
        {

            while (right > left)
            {
                if (right - left > 600)
                {
                    var n = right - left + 1;
                    var m = k - left + 1;
                    var z = Math.Log(n);
                    var s = 0.5 * Math.Exp(2 * z / 3);
                    var sd = 0.5 * Math.Sqrt(z * s * (n - s) / n) * (m - n / 2 < 0 ? -1 : 1);
                    int newLeft = Math.Max(left, (int)Math.Ceiling((float)(k - m * s / n + sd)));
                    int newRight = Math.Min(right, (int)Math.Ceiling((float)(k + (n - m) * s / n + sd)));
                    quickselect(arr, k, newLeft, newRight, compare);
                }

                var t = arr[k];
                var i = left;
                var j = right;

                swap(arr, left, k);
                if (compare.Compare(arr[right], t) > 0) swap(arr, left, right);

                while (i < j)
                {
                    swap(arr, i, j);
                    i++;
                    j--;
                    while (compare.Compare(arr[i], t) < 0) i++;
                    while (compare.Compare(arr[j], t) > 0) j--;
                }

                if (compare.Compare(arr[left], t) == 0) swap(arr, left, j);
                else
                {
                    j++;
                    swap(arr, j, right);
                }

                if (j <= k) left = j + 1;
                if (k <= j) right = j - 1;
            }
        }
#region BuildTree
        private node<U> BuildTree(U[] data)
        {
            var treeHeight = GetDepth(data.Length);
            return BuildNodes(data.ToList(), 0, data.Length - 1, treeHeight);
        }
        private int GetDepth(int numNodes)
        {
            return (int)Math.Ceiling(Math.Log(numNodes) / Math.Log(_maxEntries));
        }
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private node<U> BuildNodes(List<U> data, int left, int right, int height)
        {
            node<U> node;
            var num = right - left + 1;

            if (num <= _maxEntries)
            {
                var ll = data.slice(left, right + 1);
                node = new node<U>(ll);
                calcBBox(node, toBBox);
                return node;

            }
            node =new node<U>();
            node.leaf = false;
            node.height = height;

            var nodeSize = (num + (_maxEntries - 1)) / _maxEntries;
            var subSortLength = nodeSize * (int)Math.Ceiling(Math.Sqrt(_maxEntries));
            //sw.Start();
           // data.Sort(left, right - left + 1, compareY);
            multiSelect(data, left, right, subSortLength, compareX);
           // sw.Stop();
            for (int subCounter = left; subCounter <= right; subCounter += subSortLength)
            {
                var subRight = Math.Min(subCounter + subSortLength - 1, right);
               // data.Sort(subCounter, subRight - subCounter + 1, compareY);
               // sw.Start();
                multiSelect(data, subCounter, subRight, nodeSize, compareY);
                //sw.Stop();
                for (int nodeCounter = subCounter; nodeCounter <= subRight; nodeCounter += nodeSize)
                {
                    node.children.Add(
                        BuildNodes(
                            data,
                            nodeCounter,
                            Math.Min(nodeCounter + nodeSize - 1, subRight),
                            height - 1
                            ));
                }
            }
            calcBBox(node,toBBox);
            return node;
        }
#endregion
    }

    public class node<T> : FastPriorityQueueNode where T : spatialAttribute
    {
        public int height = 1;
        public bool leaf { set { if (!value) elements = null; else throw new Exception("access unexcept"); } get { return elements != null; } }
        public double minX = double.PositiveInfinity, minY = double.PositiveInfinity, maxY = double.NegativeInfinity, maxX = double.NegativeInfinity;

        public List<T>  elements = null;
        public node(List<node<T>> node)
        {
            children = node;
        }
        public node(List<T> elements)
        {
            this.elements = elements;
        }
        public node(T element)
        {
            this.elements =new List<T>(elements);
        }
        public node()
        {
            
        }
        public float dist(T p)
        {
            return (float)boxDist(p,this);
        }
        static double boxDist(T p, node<T> box)
        {
            double x = p.x, y = p.y;
            double dx = axisDist(x, box.minX, box.maxX),
                dy = axisDist(y, box.minY, box.maxY);
            return dx * dx + dy * dy;
        }

        static double axisDist(double k, double min, double max)
        {
            return k < min ? min - k : k <= max ? 0 : k - max;
        }
        public static implicit operator bool(node<T> a)
        {
            return a != null;
        }
        public List<node<T>> children = new List<node<T>>(10);
        
    }
   public interface spatialAttribute
    {
        double x { get; set; }
        double y { get; set; }
       //double dis(spatialAttribute spatialele, spatialAttribute spatial2);
    }

}