using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Adt;
using Newtonsoft.Json;

namespace AdtAPI
{
    public static class API
    {
        #region Fields

        static readonly string adtConnectionString;
        static readonly string elasticSearchConnectionString;

        #endregion

        #region Constructor

        static API()
        {
            adtConnectionString = ConfigurationManager.ConnectionStrings["Adt"].ConnectionString;
            elasticSearchConnectionString = ConfigurationManager.ConnectionStrings["AdtElasticSearch"].ConnectionString;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        #endregion

        #region Ward Methods

        public static async Task<Ward> GetWardByCampusIdAndId(string campusId, string id)
        {
            id = id.Maybe(s => s.PadLeft(4, '0'));

            return (await GetDocsFromCouchDb<Ward>("zis_ward", string.Concat(campusId, "-", id)).ConfigureAwait(false)).FirstOrDefault();
        }

        public static async Task<Ward> GetWardById(string id)
        {
            id = id.Maybe(s => s.PadLeft(4, '0'));

            return (await GetDocsFromCouchDb<Ward>("zis_ward", id, view: "by_id").ConfigureAwait(false)).FirstOrDefault();
        }

        public static async Task<IEnumerable<Ward>> GetAllWards()
        {
            return await GetDocsFromCouchDb<Ward>("zis_ward").ConfigureAwait(false);
        }

        #endregion

        #region Doctor Methods

        public static async Task<Doctor> GetDoctorById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            id = id.Maybe(s => s.PadLeft(6, '0'));

            return (await GetDocsFromCouchDb<Doctor>("zis_doctor", id).ConfigureAwait(false)).FirstOrDefault();
        }

        public static async Task<IEnumerable<Doctor>> GetDoctorsByIds(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any())
                return Enumerable.Empty<Doctor>();

            ids = (from id in ids select id.Maybe(s => s.PadLeft(6, '0')));

            return await GetDocsFromCouchDb<Doctor>("zis_doctor", keys: ids).ConfigureAwait(false);
        }

        public static async Task<Doctor> GetDoctorByRizivNr(string rizivNr)
        {
            if (string.IsNullOrEmpty(rizivNr))
                return null;

            rizivNr = rizivNr.Maybe(s => s.Trim());

            return (await GetDocsFromCouchDb<Doctor>("zis_doctor", rizivNr, view: "by_rizivnumber").ConfigureAwait(false)).FirstOrDefault();
        }

        public static async Task<IEnumerable<Doctor>> GetDoctorsByRizivNrs(IEnumerable<string> rizivNrs)
        {
            if (rizivNrs == null || !rizivNrs.Any())
                return Enumerable.Empty<Doctor>();

            rizivNrs = (from rizivNr in rizivNrs select rizivNr.Maybe(s => s.Trim()));

            return await GetDocsFromCouchDb<Doctor>("zis_doctor", keys: rizivNrs, view: "by_rizivnumber").ConfigureAwait(false);
        }

        public static async Task<IEnumerable<Doctor>> GetAllDoctors()
        {
            return await GetDocsFromCouchDb<Doctor>("zis_doctor").ConfigureAwait(false);
        }

        public static async Task<IEnumerable<Doctor>> GetDoctorsByQueryDsl(string queryDsl, int maxResults = 100)
        {
            return await GetDocsFromElasticSearch<Doctor>("zis_doctor", queryDsl, maxResults).ConfigureAwait(false);
        }

        #endregion

        #region Photo Methods

        public static async Task<byte[]> GetPhotoByPatientId(string patientId)
        {
            if (string.IsNullOrEmpty(patientId))
                return new byte[0];

            return await GetDocAttachmentFromCouchDb("zis_photo", patientId, "eid.jpg").ConfigureAwait(false);
        }

        #endregion

        #region Patient Methods

        public static async Task<Patient> GetPatientById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            id = id.Maybe(s => s.PadLeft(10, '0'));

            var patient = (await GetDocsFromCouchDb<Patient>("zis_patient", id).ConfigureAwait(false)).FirstOrDefault();
            if (patient != null && !patient.MergedWithId.IsNullOrEmpty())
                return await GetPatientById(patient.MergedWithId).ConfigureAwait(false);
            return patient;
        }

        public static async Task<IEnumerable<Patient>> GetPatientsByIds(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any())
                return Enumerable.Empty<Patient>();

            ids = (from id in ids select id.Maybe(s => s.PadLeft(10, '0')));

            return await GetDocsFromCouchDb<Patient>("zis_patient", keys: ids).ConfigureAwait(false);
        }

        public static async Task<Patient> GetPatientBySSN(string ssn)
        {
            if (string.IsNullOrEmpty(ssn))
                return null;

            ssn = ssn.Maybe(s => s.Trim());

            return (await GetDocsFromCouchDb<Patient>("zis_patient", ssn, view: "by_ssn").ConfigureAwait(false)).FirstOrDefault();
        }

        public static async Task<IEnumerable<Patient>> GetPatientsBySSNs(IEnumerable<string> rizivNrs)
        {
            if (rizivNrs == null || !rizivNrs.Any())
                return Enumerable.Empty<Patient>();

            rizivNrs = (from rizivNr in rizivNrs select rizivNr.Maybe(s => s.Trim()));

            return await GetDocsFromCouchDb<Patient>("zis_patient", keys: rizivNrs, view: "by_ssn").ConfigureAwait(false);
        }

        public static async Task<IEnumerable<Patient>> GetPatientsByQueryDsl(string queryDsl, int maxResults = 100)
        {
            return await GetDocsFromElasticSearch<Patient>("zis_patient", queryDsl, maxResults).ConfigureAwait(false);
        }

        #endregion

        #region Visit Methods

        public static async Task<Visit> GetVisitById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            id = id.Maybe(s => s.PadLeft(8, '0'));

            return (await GetDocsFromCouchDb<Visit>("zis_visit", id).ConfigureAwait(false)).FirstOrDefault();
        }

        public static async Task<IEnumerable<Visit>> GetVisitsByIds(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any())
                return Enumerable.Empty<Visit>();

            ids = (from id in ids select id.Maybe(s => s.PadLeft(8, '0')));

            return await GetDocsFromCouchDb<Visit>("zis_visit", keys: ids).ConfigureAwait(false);
        }

        public static async Task<IEnumerable<Visit>> GetVisitsByPatientId(string patientId)
        {
            if (string.IsNullOrEmpty(patientId))
                return null;

            patientId = patientId.Maybe(s => s.PadLeft(10, '0'));

            return await GetDocsFromCouchDb<Visit>("zis_visit", patientId, view: "by_patid").ConfigureAwait(false);
        }

        public static async Task<IEnumerable<Visit>> GetVisitsByPatientIds(IEnumerable<string> patientIds)
        {
            if (patientIds == null || !patientIds.Any())
                return Enumerable.Empty<Visit>();

            patientIds = (from patientId in patientIds select patientId.Maybe(s => s.PadLeft(10, '0')));

            return await GetDocsFromCouchDb<Visit>("zis_visit", keys: patientIds, view: "by_patid").ConfigureAwait(false);
        }

        #endregion

        #region WebClient Methods

        static async Task<IEnumerable<T>> GetDocsFromElasticSearch<T>(string index, string queryDsl, int size = 5, int from = 0)
        {
            var uri = new Uri(elasticSearchConnectionString);
            var requestUri = string.Format(@"{0}/_search?size={1}&from={2}", index, size, from);
            using (var client = uri.CreateHttpClient())
            {
                using (var content = new StringContent(queryDsl, Encoding.UTF8, "application/json"))
                {
                    //using (var compressedContent = new GZipCompressedContent(content)) //ELASTICSEARCH DOESN'T SUPPORT COMPRESSION!!!!!!!!
                    //{
                        using (var response = await client.PostAsync(requestUri, /*compressedContent*/content).ConfigureAwait(false))
                        {
                            if (!response.IsSuccessStatusCode)
                                return Enumerable.Empty<T>();
                            var responseContentAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var result = JsonConvert.DeserializeObject<Adt.ElasticSearch.Result<T>>(responseContentAsString.Replace(Environment.NewLine, string.Empty));
                            return (from hit in result.hits.hits
                                    where hit._source != null
                                    select hit._source);
                        }
                    //}
                }
            }
        }

        static async Task<IEnumerable<T>> GetDocsFromCouchDb<T>(string database, string key = null, IEnumerable<string> keys = null, string view = null)
        {
            HttpContent content = null;
            GZipCompressedContent compressedContent = null;
            var uri = new Uri(adtConnectionString);
            var requestUri = view.IsNullOrEmpty() ?
                string.Format(@"{0}/_all_docs?include_docs=true", database) :
                string.Format(@"{0}/_design/docs/_view/{1}?include_docs=true", database, view);
            if (!key.IsNullOrEmpty())
                requestUri += string.Format(@"&key=""{0}""", key);
            if (keys != null)
            {
                content = new StringContent(JsonConvert.SerializeObject(new { keys = keys }), Encoding.UTF8, "application/json");
                compressedContent = new GZipCompressedContent(content);
            }
            using (var client = uri.CreateHttpClient())
            {
                using (var response = compressedContent == null
                    ? await client.GetAsync(requestUri).ConfigureAwait(false)
                    : await client.PostAsync(requestUri, compressedContent).ConfigureAwait(false))
                {
                    if (compressedContent != null)
                        compressedContent.Dispose();
                    if (content != null)
                        content.Dispose();
                    if (!response.IsSuccessStatusCode)
                        return Enumerable.Empty<T>();
                    var responseContentAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var result = JsonConvert.DeserializeObject<Adt.CouchDb.Result<T>>(responseContentAsString.Replace(Environment.NewLine, string.Empty));
                    return (from row in result.rows
                            where row.doc != null
                            select row.doc);
                }
            }
        }

        static async Task<byte[]> GetDocAttachmentFromCouchDb(string database, string key, string attachmentName)
        {
            var uri = new Uri(adtConnectionString);
            var requestUri = string.Format(@"{0}/{1}/{2}", database, key, attachmentName);
            using (var client = uri.CreateHttpClient())
            {
                using (var response = await client.GetAsync(requestUri).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        return new byte[0];
                    return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }
            }
        }

        #endregion
    }
}
