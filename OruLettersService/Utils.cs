﻿using System;
using System.Globalization;

namespace OruLettersService
{
    internal static class Utils
    {
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
    }
}
