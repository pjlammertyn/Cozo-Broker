using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OruLabo;
using Newtonsoft.Json;

namespace OruLaboAPI
{
    public static class API
    {
        #region Fields

        static readonly string oruLaboConnectionString;

        #endregion

        #region Constructor

        static API()
        {
            oruLaboConnectionString = ConfigurationManager.ConnectionStrings["OruLabo"].ConnectionString;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        #endregion

       #region CommonOrder Methods

        public static async Task<CommonOrder> GetCommonOrderById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            id = id.Maybe(s => s.PadLeft(8, '0'));

            return (await GetDocsFromCouchDb<CommonOrder>("zis_CommonOrder", id).ConfigureAwait(false)).FirstOrDefault();
        }

        public static async Task<IEnumerable<CommonOrder>> GetCommonOrdersByIds(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any())
                return Enumerable.Empty<CommonOrder>();

            ids = (from id in ids select id.Maybe(s => s.PadLeft(8, '0')));

            return await GetDocsFromCouchDb<CommonOrder>("zis_CommonOrder", keys: ids).ConfigureAwait(false);
        }

        public static async Task<IEnumerable<CommonOrder>> GetCommonOrdersByPatientId(string patientId)
        {
            if (string.IsNullOrEmpty(patientId))
                return null;

            patientId = patientId.Maybe(s => s.PadLeft(10, '0'));

            return await GetDocsFromCouchDb<CommonOrder>("zis_CommonOrder", patientId, view: "by_patid").ConfigureAwait(false);
        }

        public static async Task<IEnumerable<CommonOrder>> GetCommonOrdersByPatientIds(IEnumerable<string> patientIds)
        {
            if (patientIds == null || !patientIds.Any())
                return Enumerable.Empty<CommonOrder>();

            patientIds = (from patientId in patientIds select patientId.Maybe(s => s.PadLeft(10, '0')));

            return await GetDocsFromCouchDb<CommonOrder>("zis_CommonOrder", keys: patientIds, view: "by_patid").ConfigureAwait(false);
        }

        #endregion
    }
}
