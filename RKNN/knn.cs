using Priority_Queue;
using RTREE;
using System;
using System.Collections.Generic;

namespace RKNN
{
    class Point : Priority_Queue.FastPriorityQueueNode, spatialAttribute
    {
        public double x { get { return x; } set { x=value; }}
        public double y { get { return y; } set { y = value; } }
        public Point(double x,double y)
        {
            this.x = x;
            this.y = y;
        }
       public static float distence(Point p1, Point p2)
        {
            float dx = (float)(p1.x - p2.x);
            float dy = (float)(p1.y - p2.y);
            return dx * dx + dy * dy;
        }
    }
    class PointKnn
    {
        static FastPriorityQueue<FastPriorityQueueNode> q;
        static Point nearstpoint(Point p, RBush<Point> pointset)
        {
            q.Clear();
            var node = pointset.data;
            FastPriorityQueueNode pre = new FastPriorityQueueNode();
            pre.Priority = float.PositiveInfinity;
            while (true)
            {
                if (node.leaf)
                {
                    for (int i = 0; i < node.elements.Count; i++)
                    {
                        float d =Point.distence(p, node.elements[i]);
                        if (d != 0)
                            q.Enqueue(node.elements[i], d);
                    }
                }
                else
                {
                    for (int i = 0; i < node.children.Count; i++)
                    {
                        q.Enqueue(node.children[i], (float)boxDist(p, node.children[i]));
                    }
                }
                var curr = q.Dequeue();

                if (!curr.element)
                    node = curr as node<Point>;
                else
                {
                    //if(constrainlist!=null&&!constrainlist.Contains(curr as Point))
                    return curr as Point;
                }
            }
        }
        static double boxDist(Point p, node<Point> box)
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
        static Point secondnearstpoints(Point p, RBush<Point> pointset)
        {
            q.Clear();
            var node = pointset.data;
            FastPriorityQueueNode pre = new FastPriorityQueueNode();
            pre.Priority = float.PositiveInfinity;
            int pindex = 0;
            //int end = points.Length - 1;
            while (true)
            {
                if (node.leaf)
                {
                    for (int i = 0; i < node.elements.Count; i++)
                    {
                        float d =Point.distence(p, node.elements[i]);
                        if (d != 0)
                            q.Enqueue(node.elements[i], d);
                    }
                }
                else
                {
                    for (int i = 0; i < node.children.Count; i++)
                    {
                        q.Enqueue(node.children[i], (float)boxDist(p, node.children[i]));
                    }
                }
                var curr = q.Dequeue();

                if (!curr.element)
                    node = curr as node<Point>;
                else
                {
                    var p2 = curr as Point;
                    if (pindex > 0)
                    {
                        return p2;
                    }
                    else
                    {
                        pindex++;
                    }
                }
            }
        }
    }
}
