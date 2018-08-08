using System;
using System.Collections.Generic;

namespace Extensions
{
    public static class ListExtensions
    {
        //get count number element from index this operation will change origin 
        public static List<T> Splice<T>(this List<T> list, int index, int count)
        {
            List<T> range = list.GetRange(index, count);
            list.RemoveRange(index, count);
            return range;
        }
        public static List<T> slice<T>(this List<T> list, int index, int end)
        {
            var count = end - index ;
            List<T> range = list.GetRange(index, count);
            return range;
        }
        public static T pop<T>(this List<T> list)
        {
            int i = list.Count - 1;
            var count = list[i];
            list.RemoveAt(i);
            return count;
        }
    }
}