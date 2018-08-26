# R-tree-based-KNN
The Knn.cs demonstrate How to use KNN and implementation
for now the tree mainly suit for 2d space.

initiazation by input parameter two comparer. The toBBox parameter Func get one leaf parent node as input,which means that you should alter input's max and min encompass manifold or voxel as show below as 2d point rectangle.



 Func<node<Point>, node<Point>> toBBox = (a) => {

            for (int i = 0; i < a.elements.Count; i++)
            {
                Point ele = a.elements[i];
                a.maxX = Math.Max(a.maxX, ele.x);
                a.minX = Math.Min(a.minX, ele.x);
                a.minY = Math.Min(a.minY, ele.y);
                a.maxY = Math.Max(a.maxY, ele.y);
            }
            return a;
        };
the item should set the 'element=true' in construct method
