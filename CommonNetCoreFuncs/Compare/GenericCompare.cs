using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Compare
{
    public class GenericCompare<T>: IEqualityComparer<T>
    {
        public bool Equals(T obj1, T obj2)
        {
            // They're both null.
            if (obj1 == null && obj2 == null) return true;
            // One is null, so they can't be the same.
            if (obj1 == null || obj2 == null) return false;
            // How can they be the same if they're different types?
            if (obj1.GetType() != obj1.GetType()) return false;
            var Props = obj1.GetType().GetProperties();
            foreach (var Prop in Props)
            {
                var aPropValue = Prop.GetValue(obj1) ?? string.Empty;
                var bPropValue = Prop.GetValue(obj2) ?? string.Empty;
                if (aPropValue.ToString() != bPropValue.ToString())
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(T obj)
        {
            var Props = obj.GetType().GetProperties();
            string allProps = null;
            foreach (var Prop in Props)
            {
                var propValue = Prop.GetValue(obj) ?? string.Empty;
                allProps += propValue;
            }
            return allProps.GetHashCode();
        }

        //From: https://www.c-sharpcorner.com/article/introduction-to-generic-iequalitycomparer/
        //private Func<T, object> _virtualFieldComparator;
        //private Func<T, T, bool> _virtualFilterComparator;
        //public GenericCompare(Func<T, object> virtualFieldComparator)
        //{
        //    if (virtualFieldComparator == null) throw new ArgumentNullException(nameof(virtualFieldComparator), $"{nameof(virtualFieldComparator)}  doesn't be null");
        //    Reset();
        //    this._virtualFieldComparator = virtualFieldComparator;
        //}
        //public GenericCompare(Func<T, T, bool> virtualFilterComparator)
        //{
        //    if (virtualFilterComparator == null) throw new ArgumentNullException(nameof(virtualFilterComparator), $"{nameof(virtualFilterComparator)}  doesn't be null");
        //    Reset();
        //    this._virtualFilterComparator = virtualFilterComparator;
        //}
        //private void Reset()
        //{
        //    _virtualFieldComparator = null;
        //    _virtualFilterComparator = null;
        //}
        //public bool Equals(T x, T y)
        //{
        //    bool result = false;
        //    if (_virtualFieldComparator != null) result = _virtualFieldComparator(x).Equals(_virtualFieldComparator(y));
        //    else result = _virtualFilterComparator(x, y);
        //    return result;
        //}
        //public int GetHashCode(T obj)
        //{
        //    int result = 0;
        //    if (_virtualFieldComparator != null) result = _virtualFieldComparator(obj).GetHashCode();
        //    else result = _virtualFilterComparator(obj, obj).GetHashCode();
        //    return result;
        //}
    }
}
