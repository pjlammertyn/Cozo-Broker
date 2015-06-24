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
    public class GenericRestCrudHttpClient : GenericRestGetHttpClient
    {
        #region Fields

        static readonly ILog log = LogManager.GetLogger(typeof(GenericRestCrudHttpClient));
       
        #endregion

        #region Constructor

        public GenericRestCrudHttpClient(string uriString, bool getFromCache = false, bool compressContent = true)
            : base(uriString, getFromCache, compressContent)
        {
            hashCodeCache = new MemoryCache("hashCode");
        }

        #endregion

        #region Methods

        public async Task<string> HeadAsync(string requestUri, bool fromCache = false)
        {
            if (fromCache && eTagsCache.Contains(requestUri))
            {
                if (log.IsDebugEnabled)
                    log.DebugFormat("HEAD FROM CACHE {0}", requestUri);
                return eTagsCache[requestUri] as string;
            }

            using (var request = new HttpRequestMessage(HttpMethod.Head, requestUri))
            {
                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        if (log.IsDebugEnabled)
                            log.DebugFormat("HEAD NOT FOUND {0}", requestUri);
                        return null;
                    }

                    response.EnsureSuccessStatusCode();

                    if (log.IsInfoEnabled)
                        log.InfoFormat("HEAD {0}", requestUri);

                    var eTag = response.Headers.ETag != null ? response.Headers.ETag.Tag : null;
                    if (!string.IsNullOrEmpty(eTag))
                    {
                        if (!eTagsCache.Contains(requestUri) || eTagsCache[requestUri] as string != eTag)
                        {
                            objectCache.Remove(requestUri);
                                hashCodeCache.Remove(requestUri);
                        }
                        eTagsCache.Set(requestUri, eTag, slidingExpirationPolicy);
                    }
                    else
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("No eTag returned after HEAD {0}", requestUri);
                    }
                    return eTag;
                }
            }
        }

        public async Task<string> PutAsync<T>(string requestUri, T value, string mediaType = "application/json", string eTag = null)
            where T : class
        {
            if (string.IsNullOrEmpty(eTag) && eTagsCache.Contains(requestUri))
                eTag = eTagsCache[requestUri] as string;

            if (hashCodeCache.Contains(requestUri))
            {
                var cachedHashCode = hashCodeCache[requestUri] as byte[];
                if (cachedHashCode != default(byte[]))
                {
                    var hashCode = value.ToByteArray().ComputeHash();
                    if (cachedHashCode.SequenceEqual(hashCode))
                    {
                        if (log.IsDebugEnabled)
                            log.DebugFormat("PUT NOT MODIFIED {0}", requestUri);
                        return eTag;
                    }
                }
            }

            HttpContent content = null;
            try
            {
                if (typeof(T) == typeof(byte[]))
                {
                    content = new ByteArrayContent(value as byte[]);
                    content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
                }
                else
                {
                    var json = JsonConvert.SerializeObject(value);
                    content = new StringContent(json, Encoding.UTF8, mediaType);
                }

                if (compressContent)
                    content = new GZipCompressedContent(content);

                using (var request = new HttpRequestMessage(HttpMethod.Put, requestUri))
                {
                    if (!string.IsNullOrEmpty(eTag))
                        request.Headers.IfMatch.Add(new EntityTagHeaderValue(eTag));

                    request.Content = content;
                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (response.StatusCode == HttpStatusCode.Conflict)
                        {
                            if (log.IsWarnEnabled)
                                log.WarnFormat("PUT CONFLICT {0}", requestUri);

                            eTagsCache.Remove(requestUri);
                            objectCache.Remove(requestUri);
                                hashCodeCache.Remove(requestUri);

                            eTag = await HeadAsync(requestUri);
                            if (!string.IsNullOrEmpty(eTag))
                                eTagsCache.Set(requestUri, eTag, slidingExpirationPolicy);

                            return await PutAsync(requestUri, value, mediaType, eTag);
                        }

                        if (log.IsInfoEnabled)
                            log.Info(string.Concat("PUT ", string.IsNullOrEmpty(eTag) ? "NEW " : string.Empty, requestUri));

                        response.EnsureSuccessStatusCode();

                        eTag = response.Headers.ETag.Tag;
                        if (!string.IsNullOrEmpty(eTag))
                        {
                            eTagsCache.Set(requestUri, eTag, slidingExpirationPolicy);
                            objectCache.Set(requestUri, value, slidingExpirationPolicy);
                                hashCodeCache.Set(requestUri, value.ToByteArray().ComputeHash(), slidingExpirationPolicy);
                        }
                        else
                        {
                            if (log.IsWarnEnabled)
                                log.WarnFormat("No eTag returned after PUT {0}", requestUri);
                        }

                        return eTag;
                    }
                }
            }
            finally
            {
                content.Dispose();
            }
        }

        public async Task DeleteAsync(string requestUri)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Delete, requestUri))
            {
                if (eTagsCache.Contains(requestUri))
                    request.Headers.IfMatch.Add(new EntityTagHeaderValue(eTagsCache[requestUri] as string));

                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("DELETE NOT FOUND {0}", requestUri);
                    }
                    else
                    {
                        if (log.IsInfoEnabled)
                            log.Info(string.Concat("DELETE ", !eTagsCache.Contains(requestUri) ? "NOT IN CACHE " : string.Empty, requestUri));

                        response.EnsureSuccessStatusCode();
                    }

                    eTagsCache.Remove(requestUri);
                    objectCache.Remove(requestUri);
                        hashCodeCache.Remove(requestUri);
                }
            }
        }

        #endregion
    }
}
