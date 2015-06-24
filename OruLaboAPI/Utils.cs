using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace OruLaboAPI
{
    internal static class Utils
    {
        internal static V Maybe<T, V>(this T t, Func<T, V> selector)
        {
            return t != null ? selector(t) : default(V);
        }

        internal static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }
    }
}
