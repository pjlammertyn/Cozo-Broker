using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using HL7ServiceBase;
using RestClient;

namespace AdtService
{
    class HL7Poller : HL7PollerBase
    {
        #region Fields

        static readonly string restConnectionString;
        GenericRestCrudHttpClient httpClient;
        Timer houseKeepingTimer;

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
            houseKeepingTimer = new Timer();
            houseKeepingTimer.Interval = 1000 * 60 * 60; //1 hour
            houseKeepingTimer.Elapsed += houseKeepingTimer_Elapsed;
        }

        #endregion

        #region HL7PollerBase Implementation

        public override void Start()
        {
            var t = Task.Run(async () =>
            {
                using (var hl7Parser = new HL7Parser(httpClient))
                    await hl7Parser.FillWardCache();
            });
            t.Wait();
            //houseKeepingTimer_Elapsed(null, null);
            base.Start();
            houseKeepingTimer.Start();
        }

        public override void Stop()
        {
            base.Stop();
            houseKeepingTimer.Stop();
        }

        protected override async Task ProcessFile(Stream stream)
        {
            using (var hl7Parser = new HL7Parser(stream, httpClient))
                await hl7Parser.Parse();
        }

        protected override bool HandleException(Exception ex)
        {
            httpClient.ClearCache();
            var t = Task.Run(async () =>
            {
                using (var hl7Parser = new HL7Parser(httpClient))
                    await hl7Parser.FillWardCache();
            });
            t.Wait();
            return false;
        }

        #endregion

        #region Events

        async void houseKeepingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            using (var hl7Parser = new HL7Parser(httpClient))
            {
                await hl7Parser.FillWardCache();
                await hl7Parser.RemovBedsFromWards();
            }
        }

        #endregion
    }
}
