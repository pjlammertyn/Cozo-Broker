using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Adt
{
    public static class API
    {


        static API()
        {

        }

        static async Task<IEnumerable<dynamic>> GetDocsFromXdeCache(string database, string key = null, IEnumerable<string> keys = null, string view = null)
        {
            HttpContent content = null;
            var uri = new Uri(await GetCouchDbUri().ConfigureAwait(false));
            var requestUri = view.IsNullOrEmpty() ?
                string.Format(@"{0}/_all_docs?include_docs=true", database) :
                string.Format(@"{0}/_design/docs/_view/{1}?include_docs=true", database, view);
            if (!key.IsNullOrEmpty())
                requestUri += string.Format(@"&key=""{0}""", key);
            if (keys != null)
                content = new StringContent(JSON.SerializeDynamic(new { keys = keys }), Encoding.UTF8, "application/json");
            using (var client = uri.CreateHttpClient())
            {
                using (var response = content == null
                    ? await client.GetAsync(requestUri).ConfigureAwait(false)
                    : await client.PostAsync(requestUri, content).ConfigureAwait(false))
                {
                    if (content != null)
                        content.Dispose();
                    if (!response.IsSuccessStatusCode)
                        return Enumerable.Empty<dynamic>();
                    var responseContentAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JSON.DeserializeDynamic(responseContentAsString.Replace(Environment.NewLine, string.Empty));
                    var rows = result["rows"];
                    return (from row in (IEnumerable<dynamic>)result["rows"]
                            where row.ContainsKey("doc")
                            select row["doc"]);
                }
            }
        }
    }
}
