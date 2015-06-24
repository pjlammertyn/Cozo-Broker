using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HL7ServiceBase;
using log4net;
using OruLettersService.Dto;

namespace OruLettersService
{
    class HL7Parser : HL7ParserBase
    {
        #region Fields

        static readonly ILog log = LogManager.GetLogger(typeof(HL7Parser));
        static readonly string letterUri;

        string patientId;
        string visitId;
        Letter letter;

        #endregion

        #region Constructor

        static HL7Parser()
        {
            letterUri = ConfigurationManager.AppSettings["LetterUri"];
        }

        public HL7Parser(Stream stream)
            : base(stream)
        {
        }

        #endregion

        #region HL7ParserBase implementation

        protected override async Task ProcessSegment(string line)
        {
            var segmentId = line.Substring(0, 3);
            switch (segmentId)
            {
                case "MSH":
                    ProcessMSH(line);
                    break;
                case "PID":
                    ProcessPID(line);
                    break;
                case "PV1":
                    ProcessPV1(line);
                    break;
                case "OBR":
                    await ProcessOBR(line);
                    break;
                case "OBX":
                    ProcessOBX(line);
                    break;
                default:
                    break;
            }

            await Task.Delay(0);
        }

        protected override async Task StoreDocuments()
        {
            if (letter != null)
                await httpClient.PutAsync(string.Format(letterUri, letter.FillerOrderNumber), letter);
        }

        #endregion

        #region Methods


        #endregion

        #region HL7 Segment Methods

        void ProcessMSH(string segment)
        {
            fieldSep = segment[3];
            componentSep = segment[4];
            repetitionSep = segment[5];
            escapeChar = segment[6];
            subComponentSep = segment[7];

            var fields = segment.Split(fieldSep);
        }

        void ProcessPID(string segment)
        {
            var fields = segment.Split(fieldSep);

            patientId = GetValueFromField(fields.ElementAtOrDefault(3));
        }

        void ProcessPV1(string segment)
        {
            var fields = segment.Split(fieldSep);

            visitId = GetValueFromField(fields.ElementAtOrDefault(19));
        }

        async Task ProcessOBR(string segment)
        {
            var fields = segment.Split(fieldSep);

            var fillerOrderNumber = GetValueFromField(fields.ElementAtOrDefault(3));
            letter = await httpClient.GetAsync<Letter>(string.Format(letterUri, fillerOrderNumber));
            if (letter == null)
                letter = new Letter() { FillerOrderNumber = fillerOrderNumber };

            letter.PatientId = patientId;
            letter.VisitId = visitId;

            letter.FillerNamespace = GetValueFromField(fields.ElementAtOrDefault(3), componentIndex: 1);
            letter.UniversalServiceId = GetValueFromField(fields.ElementAtOrDefault(4));
            letter.UniversalServiceText = GetValueFromField(fields.ElementAtOrDefault(4), componentIndex: 1);
            letter.ObservationDate = GetValueFromField(fields.ElementAtOrDefault(7)).Maybe(s => s.ToNullableDatetime("yyyyMMddHHmmss"));
            letter.OrderingProviderId = GetValueFromField(fields.ElementAtOrDefault(16));
            letter.DiagnosticServiceSectionId = GetValueFromField(fields.ElementAtOrDefault(24));
        }

        void ProcessOBX(string segment)
        {
            var fields = segment.Split(fieldSep);

            letter.ValueType = GetValueFromField(fields.ElementAtOrDefault(2));
            letter.ObservationId = GetValueFromField(fields.ElementAtOrDefault(3));
            letter.ObservationText = GetValueFromField(fields.ElementAtOrDefault(3), componentIndex: 1);
            letter.AbnormalFlags = GetValueFromField(fields.ElementAtOrDefault(8));
            letter.ObservationDate = letter.ObservationDate ?? GetValueFromField(fields.ElementAtOrDefault(14)).Maybe(s => s.ToNullableDatetime("yyyyMMddHHmmss"));
            letter.ResultStatus = GetValueFromField(fields.ElementAtOrDefault(11));
        }

        #endregion
    }
}
