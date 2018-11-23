using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetGenerator.Runtime.ExtensionMethods
{
    public static class IEnumerableExtensionMethods
    {
        public static int IndexOf<T>(this IEnumerable<T> source, T value)
        {
            return source.IndexOf<T>(value, EqualityComparer<T>.Default);

        }

        public static int IndexOf<T>(this IEnumerable<T> source, T value, IEqualityComparer<T> comparer)
        {
            int index = 0;
            foreach(var item in source)
            {
                if (comparer.Equals(item, value))
                {
                    return index;
                }
                ++index;
            }

            return -1;
        }
    }
}
