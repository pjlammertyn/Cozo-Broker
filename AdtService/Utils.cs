using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace AdtService
{
    internal static class Utils
    {
        internal static bool ToBool(this string value)
        {
            bool result = false;

            if (!string.IsNullOrEmpty(value))
                bool.TryParse(value, out result);

            return result;
        }

        internal static int? ToNullableInt(this string value)
        {
            int result;

            if (!string.IsNullOrEmpty(value) && Int32.TryParse(value, out result))
                return result;

            return null;
        }

        internal static int FindIndex<T>(this IList<T> source, Predicate<T> match)
        {
            if (source == null)
                return -1;
            for (int i = 0; i < source.Count; i++)
                if (match(source[i]))
                    return i;
            return -1;
        }
        
        internal static V Maybe<T, V>(this T t, Func<T, V> selector)
        {
            return t != null ? selector(t) : default(V);
        }

        internal static DateTime? ToNullableDatetime(this string value, params string[] formats)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            DateTime ret;
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out ret))
                    return ret;
            }

            return null;
        }

        //  Sorts an IList<T> in place.
        //internal static void Sort<T>(this IList<T> list, Comparison<T> comparison)
        //{
        //    ArrayList.Adapter((IList)list).Sort(new ComparisonComparer<T>(comparison));
        //}
    }
    
    // Wraps a generic Comparison<T> delegate in an IComparer to make it easy 
    // to use a lambda expression for methods that take an IComparer or IComparer<T>
    //internal class ComparisonComparer<T> : IComparer<T>, IComparer
    //{
    //    readonly Comparison<T> _comparison;

    //    public ComparisonComparer(Comparison<T> comparison)
    //    {
    //        _comparison = comparison;
    //    }

    //    public int Compare(T x, T y)
    //    {
    //        return _comparison(x, y);
    //    }

    //    public int Compare(object o1, object o2)
    //    {
    //        return _comparison((T)o1, (T)o2);
    //    }
    //}
}
