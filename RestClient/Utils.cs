using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace RestClient
{
    internal static class Utils
    {
        internal static byte[] ToByteArray(this Object objectToSerialize)
        {
            var formatter = new BinaryFormatter();
            using (var fs = new MemoryStream())
            {
                formatter.Serialize(fs, objectToSerialize);
                return fs.ToArray();
            }
        }

        internal static byte[] ComputeHash(this byte[] objectAsBytes)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                return md5.ComputeHash(objectAsBytes);
            }
        }
    }

    internal static class UriExtensions
    {
        internal static string GetAbsoluteUriExceptUserInfo(this Uri uri)
        {
            return uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.UserInfo, UriFormat.UriEscaped);
        }

        internal static string GetBasicAuthString(this Uri uri)
        {
            if (string.IsNullOrWhiteSpace(uri.UserInfo))
                return null;

            var parts = uri.GetUserInfoParts();

            var credentialsBytes = Encoding.UTF8.GetBytes(string.Format("{0}:{1}", parts[0], parts[1]));
            return Convert.ToBase64String(credentialsBytes);
        }

        internal static string[] GetUserInfoParts(this Uri uri)
        {
            return uri.UserInfo
                .Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => Uri.UnescapeDataString(p))
                .ToArray();
        }
    }
}
