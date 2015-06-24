using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using HL7ServiceBase;
using RestClient;

namespace OruLaboService
{
    class HL7Poller : HL7PollerBase
    {
        #region Fields

        static readonly string restConnectionString;
        GenericRestCrudHttpClient httpClient;

        #endregion

        #region Constructor

        static HL7Poller()
        {
            restConnectionString = ConfigurationManager.ConnectionStrings["REST"].ConnectionString;
        }

        public HL7Poller(string pollerDir, string errorDir = null)
            : base(pollerDir, errorDir)
        {
            httpClient = new GenericRestCrudHttpClient(restConnectionString, getFromCache: true);
        }

        #endregion

        #region HL7PollerBase Implementation

        protected override async Task ProcessFile(Stream stream)
        {
            using (var hl7Parser = new HL7Parser(stream, httpClient))
                await hl7Parser.Parse();
        }

        protected override bool HandleException(Exception ex)
        {
            httpClient.ClearCache();
            return false;
        }

        #endregion
    }
}
