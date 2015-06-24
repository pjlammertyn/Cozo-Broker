using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AdtAPI
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

    internal static class UriExtensions
    {
        internal static HttpClient CreateHttpClient(this Uri uri)
        {
            var handler = new HttpClientHandler();
            if (handler.SupportsAutomaticDecompression)
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(uri.GetAbsoluteUriExceptUserInfo().TrimEnd(new[] { '/' }));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("deflate"));

            var basicAuthString = uri.GetBasicAuthString();
            if (basicAuthString != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthString);
            return client;
        }        

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

    internal class GZipCompressedContent : HttpContent
    {
        readonly HttpContent content;

        public GZipCompressedContent(HttpContent content)
        {
            this.content = content;

            this.Headers.ContentEncoding.Add("gzip");
        }


        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            using (var gzipStream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
            {
                await content.CopyToAsync(gzipStream);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;

            return false;
        }
    }
}
