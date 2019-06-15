using System;
using System.Collections.Generic;
using System.Text;

namespace Internal.AspNetCore.ReportGenerator
{
    public static class TupleComparer
    {
        public static IEqualityComparer<(T1, T2)> Create<T1, T2>(IEqualityComparer<T1> comparer1, IEqualityComparer<T2> comparer2)
        {
            return new Comparer<T1, T2>(comparer1, comparer2);
        }

        private class Comparer<T1, T2> : IEqualityComparer<(T1, T2)>
        {
            private IEqualityComparer<T1> _comparer1;
            private IEqualityComparer<T2> _comparer2;

            public Comparer(IEqualityComparer<T1> comparer1, IEqualityComparer<T2> comparer2)
            {
                _comparer1 = comparer1;
                _comparer2 = comparer2;
            }

            public bool Equals((T1, T2) x, (T1, T2) y)
            {
                return
                    _comparer1.Equals(x.Item1, y.Item1) &&
                    _comparer2.Equals(x.Item2, y.Item2);
            }

            public int GetHashCode((T1, T2) obj)
            {
                return HashCode.Combine(
                    _comparer1.GetHashCode(obj.Item1),
                    _comparer2.GetHashCode(obj.Item2));
            }
        }
    }
}
