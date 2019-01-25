using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib
{
    public interface IEnumerableWithCount<out T> : IEnumerable<T>
    {
        int Count { get; }
    }

    public class EnumerableWithCount<T> : IEnumerableWithCount<T>
    {
        private readonly IEnumerable<T> _enumerable;
        private readonly int _count;

        public EnumerableWithCount(int count, IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
            _count = count;
        }
        public int Count => _count;

        public IEnumerator<T> GetEnumerator()
        {
            return _enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _enumerable.GetEnumerator();
        }
    }
}
