using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;

namespace RestClient
{
    public class GenericRestGetHttpClient : IDisposable
    {
        #region Fields

        static readonly ILog log = LogManager.GetLogger(typeof(GenericRestGetHttpClient));
        bool disposed = false;
        protected HttpClient client;
        bool getFromCache;
        protected bool compressContent;
        protected ObjectCache eTagsCache;
        protected ObjectCache hashCodeCache;
        protected ObjectCache objectCache;
        protected static readonly CacheItemPolicy slidingExpirationPolicy;

        #endregion

        #region Constructor

        static GenericRestGetHttpClient()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            };

            TimeSpan restCacheSlidingExpirationTimeSpan;
            if (!TimeSpan.TryParse(ConfigurationManager.AppSettings["RestCacheSlidingExpirationTimeSpan"], out restCacheSlidingExpirationTimeSpan))
                restCacheSlidingExpirationTimeSpan = TimeSpan.FromHours(1);
            slidingExpirationPolicy = new CacheItemPolicy { SlidingExpiration = restCacheSlidingExpirationTimeSpan };
        }

        public GenericRestGetHttpClient(string uriString, bool getFromCache = false, bool compressContent = true)
        {
            client = CreateHttpClient(new Uri(uriString));
            this.getFromCache = getFromCache;
            this.compressContent = compressContent;
            eTagsCache = new MemoryCache("eTag");
            objectCache = new MemoryCache("object");
        }

        #endregion

        #region Methods

        public void ClearCache()
        {
            var keys = eTagsCache.Select(kvp => kvp.Key).ToList();
            foreach (string key in keys)
                eTagsCache.Remove(key);
            keys = objectCache.Select(kvp => kvp.Key).ToList();
            foreach (string key in keys)
                objectCache.Remove(key);
            if (hashCodeCache != null)
            {
                keys = hashCodeCache.Select(kvp => kvp.Key).ToList();
                foreach (string key in keys)
                    hashCodeCache.Remove(key);
            }
        }

        //TODO: implement caching in a Handler like CacheCow
        protected virtual HttpClient CreateHttpClient(Uri uri)
        {
            var handler = new HttpClientHandler(); //new WebRequestHandler();
            //handler.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.CacheIfAvailable); //ONLY CACHES GET!!!!!
            if (handler.SupportsAutomaticDecompression)
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            client = new HttpClient(handler);
            client.BaseAddress = new Uri(uri.GetAbsoluteUriExceptUserInfo().TrimEnd(new[] { '/' }));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("deflate"));

            var basicAuthString = uri.GetBasicAuthString();
            if (basicAuthString != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthString);
            return client;
        }

        public async Task<T> GetAsync<T>(string requestUri, IEnumerable<string> keys = null)
            where T : class
        {
            if (getFromCache && keys == null && objectCache.Contains(requestUri))
            {
                if (log.IsDebugEnabled)
                    log.DebugFormat("GET FROM CACHE {0}", requestUri);
                return objectCache[requestUri] as T;
            }

            if (typeof(T) == typeof(Stream))
            {
                if (log.IsInfoEnabled)
                    log.InfoFormat("GET STREAM {0}", requestUri);
                return await client.GetStreamAsync(requestUri).ConfigureAwait(false) as T;
            }

            HttpContent content = null;
            if (keys != null)
            {
                content = new StringContent(JsonConvert.SerializeObject(new { keys = keys }), Encoding.UTF8, "application/json");
                if (compressContent)
                    content = new GZipCompressedContent(content);
            }

            try
            {
                using (var request = new HttpRequestMessage(content == null ? HttpMethod.Get : HttpMethod.Post, requestUri))
                {
                    if (eTagsCache.Contains(requestUri))
                        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(eTagsCache[requestUri] as string));

                    if (content != null)
                        request.Content = content;

                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            if (log.IsDebugEnabled)
                                log.DebugFormat("GET NOT FOUND {0}", requestUri);
                            eTagsCache.Remove(requestUri);
                            objectCache.Remove(requestUri);
                            if (hashCodeCache != null)
                                hashCodeCache.Remove(requestUri);
                            return default(T);
                        }
                        if (response.StatusCode == HttpStatusCode.NotModified)
                        {
                            if (log.IsDebugEnabled)
                                log.DebugFormat("GET NOT MODIFIED {0}", requestUri);
                            return objectCache[requestUri] as T;
                        }

                        if (log.IsInfoEnabled)
                            log.InfoFormat("GET {0}", requestUri);

                        response.EnsureSuccessStatusCode();

                        var eTag = response.Headers.ETag != null ? response.Headers.ETag.Tag : null;
                        if (!string.IsNullOrEmpty(eTag))
                            eTagsCache.Set(requestUri, eTag, slidingExpirationPolicy);
                        else
                        {
                            if (log.IsWarnEnabled)
                                log.WarnFormat("No eTag returned after GET {0}", requestUri);
                        }

                        if (typeof(T) == typeof(byte[]))
                        {
                            var buffer = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                            objectCache.Set(requestUri, buffer, slidingExpirationPolicy);
                            if (hashCodeCache != null)
                                hashCodeCache.Set(requestUri, buffer.ComputeHash(), slidingExpirationPolicy);
                            return buffer as T;
                        }
                        else
                        {
                            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var obj = JsonConvert.DeserializeObject<T>(json);
                            objectCache.Set(requestUri, obj, slidingExpirationPolicy);
                            if (hashCodeCache != null)
                                hashCodeCache.Set(requestUri, obj.ToByteArray().ComputeHash(), slidingExpirationPolicy);
                            return obj;
                        }
                    }
                }
            }
            finally
            {
                if (content != null)
                    content.Dispose();
            }
        }
    
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                if (client != null)
                {
                    var hc = client;
                    client = null;
                    hc.Dispose();
                }
                disposed = true;
            }
        }

        #endregion IDisposable Members
    }

    class GZipCompressedContent : HttpContent
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
