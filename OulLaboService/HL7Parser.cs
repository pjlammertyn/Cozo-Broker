using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HL7ServiceBase;
using log4net;
using OruLabo;
using RestClient;

namespace OruLaboService
{
    class HL7Parser : HL7ParserBase
    {
        #region Fields

        static readonly ILog log = LogManager.GetLogger(typeof(HL7Parser));
        static readonly string orderUri;

        string patientId;
        string visitId;
        CommonOrder order;

        #endregion

        #region Constructor

        static HL7Parser()
        {
            orderUri = ConfigurationManager.AppSettings["OrderUri"];
        }

        public HL7Parser(Stream stream, GenericRestCrudHttpClient httpClient)
            : base(stream, httpClient)
        {
        }

        public HL7Parser(GenericRestCrudHttpClient httpClient)
            : base(httpClient)
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
                case "ORC":
                    await ProcessORC(line);
                    break;
                case "OBR":
                    ProcessOBR(line);
                    break;
                case "OBX":
                    ProcessOBX(line);
                    break;
                default:
                    break;
            }
        }

        protected override async Task StoreDocuments()
        {
            if (order != null)
                await httpClient.PutAsync(string.Format(orderUri, order.FillerOrderNumber), order);
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

        async Task ProcessORC(string segment)
        {
            var fields = segment.Split(fieldSep);

            var orderNumber = GetValueFromField(fields.ElementAtOrDefault(3));
            order = await httpClient.GetAsync<CommonOrder>(string.Format(orderUri, orderNumber));

            if (order == null)
                order = new CommonOrder() { FillerOrderNumber = orderNumber };
            else
                order.Requests = null;

            //await httpClient.HeadAsync(string.Format(orderUri, orderNumber), fromCache: true);
            //order = new CommonOrder() { FillerOrderNumber = orderNumber };

            order.PatientId = patientId;
            order.VisitId = visitId;

            order.OrderControl = GetValueFromField(fields.ElementAtOrDefault(1));
            //order.OrderControlDescription = orderControlConversionTable.GetValueOrDefault(order.OrderControl);
            order.PlacerOrderNumber = GetValueFromField(fields.ElementAtOrDefault(2));
            order.OrderStatus = GetValueFromField(fields.ElementAtOrDefault(5));
            //order.OrderStatusDescription = orderStatusConversionTable.GetValueOrDefault(order.OrderStatus);
            order.StartTiming = GetValueFromField(fields.ElementAtOrDefault(7), componentIndex: 3).Maybe(s => s.ToNullableDatetime("yyyyMMddHHmmss"));
            order.TransactionDateTime = GetValueFromField(fields.ElementAtOrDefault(9)).Maybe(s => s.ToNullableDatetime("yyyyMMddHHmmss"));
            order.OrderingProviderId = GetValueFromField(fields.ElementAtOrDefault(12));
            order.EnterersLocationPointOfCare = GetValueFromField(fields.ElementAtOrDefault(13));
            order.EnteringOrganizationId = GetValueFromField(fields.ElementAtOrDefault(17));
        }

        void ProcessOBR(string segment)
        {
            var fields = segment.Split(fieldSep);

            var observationRequest = new ObservationRequest();

            observationRequest.UniversalServiceId = GetValueFromField(fields.ElementAtOrDefault(4));
            observationRequest.SpecimenSourceCode = GetValueFromField(fields.ElementAtOrDefault(15));
            observationRequest.SpecimenSourceName = GetValueFromField(fields.ElementAtOrDefault(15), subComponentIndex: 1);
            observationRequest.ResultStatus = GetValueFromField(fields.ElementAtOrDefault(25));
            //observationRequest.ResultStatusDescription = resultStatusOBRConversionTable.GetValueOrDefault(observationRequest.ResultStatus);

            if (order.Requests == null)
                order.Requests = new List<ObservationRequest>();
            order.Requests.Add(observationRequest);
        }

        void ProcessOBX(string segment)
        {
            var fields = segment.Split(fieldSep);

            var observationResult = new ObservationResult();

            observationResult.Type = GetValueFromField(fields.ElementAtOrDefault(2));
            //observationResult.ValueTypeDescription = valueTypeConversionTable.GetValueOrDefault(observationResult.ValueType);
            observationResult.Id = GetValueFromField(fields.ElementAtOrDefault(3));
            observationResult.Text = GetValueFromField(fields.ElementAtOrDefault(3), componentIndex: 1);
            observationResult.CodingSystem = GetValueFromField(fields.ElementAtOrDefault(3), componentIndex: 2);
            observationResult.Value = GetValueFromField(fields.ElementAtOrDefault(5));
            observationResult.Units = GetValueFromField(fields.ElementAtOrDefault(6));
            //observationResult.UnitsText = GetValueFromField(fields.ElementAtOrDefault(6), componentIndex: 1);
            //observationResult.UnitsCodingSystem = GetValueFromField(fields.ElementAtOrDefault(6), componentIndex: 2);
            observationResult.ReferencesRange = GetValueFromField(fields.ElementAtOrDefault(7));
            observationResult.AbnormalFlags = GetValueFromField(fields.ElementAtOrDefault(8));
            //observationResult.AbnormalFlagsDescription = abnormalFlagsConversionTable.GetValueOrDefault(observationResult.AbnormalFlags);
            observationResult.ResultStatus = GetValueFromField(fields.ElementAtOrDefault(11));
            //observationResult.ResultStatusDescription = resultStatusOBXConversionTable.GetValueOrDefault(observationResult.ResultStatus);
            observationResult.Date = GetValueFromField(fields.ElementAtOrDefault(14)).Maybe(s => s.ToNullableDatetime("yyyyMMddHHmmss"));
            observationResult.ProducersId = GetValueFromField(fields.ElementAtOrDefault(15));

            var observationRequest = order.Requests.Last();
            if (observationRequest.Results == null)
                observationRequest.Results = new List<ObservationResult>();
            observationRequest.Results.Add(observationResult);
        }

        #endregion

        #region Conversion Tables

        //#region Abnormal Flags Conversion Table

        //static IDictionary<string, string> abnormalFlagsConversionTable = new Dictionary<string, string>()
        //{
        //    {"L", "Below low normal"},
        //    {"H", "Above high normal"},
        //    {"LL", "Below lower panic limits"},
        //    {"HH", "Above upper panic limits"},
        //    {"<", "Below absolute low-off instrument scale"},
        //    {">", "Above absolute high-off instrument scale"},
        //    {"N", "Normal (applies to non-numeric results)"},
        //    {"A", "Abnormal (applies to non-numeric results)"},
        //    {"AA", "Very abnormal (applies to non-numeric units, analogous to panic limits for numeric units)"},
        //    {"null", "No range defined, or normal ranges don't apply"},
        //    {"U", "Significant change up"},
        //    {"D", "Significant change down"},
        //    {"B", "Better--use when direction not relevant"},
        //    {"W", "Worse--use when direction not relevant"},
        //    {"S", "Susceptible"},
        //    {"R", "Resistant"},
        //    {"I", "Intermediate"},
        //    {"MS", "Moderately susceptible"},
        //    {"VS", "Very susceptible"}           
        //};

        //#endregion

        //#region Value Type Conversion Table

        //static IDictionary<string, string> valueTypeConversionTable = new Dictionary<string, string>()
        //{
        //    {"AD", "Address"},
        //    {"CE", "Coded Entry"},
        //    {"CF", "Coded Element With Formatted Values"},
        //    {"CK", "Composite ID With Check Digit"},
        //    {"CN", "Composite ID And Name"},
        //    {"CP", "Composite Price"},
        //    {"CX", "Extended Composite ID With Check Digit"},
        //    {"DT", "Date"},
        //    {"ED", "Encapsulated Data"},
        //    {"FT", "Formatted Text (Display)"},
        //    {"MO", "Money"},
        //    {"NM", "Numeric"},
        //    {"PN", "Person Name"},
        //    {"RP", "Reference Pointer"},
        //    {"SN", "Structured Numeric"},
        //    {"ST", "String Data."},
        //    {"TM", "Time"},
        //    {"TN", "Telephone Number"},
        //    {"TS", "Time Stamp (Date & Time)"},
        //    {"TX", "Text Data (Display)"},
        //    {"XAD", "Extended Address"},
        //    {"XCN", "Extended Composite Name And Number For Persons"},
        //    {"XON", "Extended Composite Name And Number For Organizations"},
        //    {"XPN", "Extended Person Number"},
        //    {"XTN", "Extended Telecommunications Number"}
        //};

        //#endregion

        //#region Result Status Conversion Table

        //static IDictionary<string, string> resultStatusOBXConversionTable = new Dictionary<string, string>()
        //{
        //    {"C", "Record coming over is a correction and thus replaces a final result"},
        //    {"D", "Deletes the OBX record"},
        //    {"F", "Final results; Can only be changed with a corrected result."},
        //    {"I", "Specimen in lab; results pending"},
        //    {"P", "Preliminary results"},
        //    {"R", "Results entered -- not verified"},
        //    {"S", "Partial results"},
        //    {"X", "Results cannot be obtained for this observation"},
        //    {"U", "Results status change to Final. without retransmitting results already sent as ‘preliminary.’ E.g., radiology changes status from preliminary to final"},
        //    {"W", "Post original as wrong, e.g., transmitted for wrong patient"}
        //};

        //static IDictionary<string, string> resultStatusOBRConversionTable = new Dictionary<string, string>()
        //{
        //    {"O", "Order received; specimen not yet received"},		
        //    {"I", "No results available; specimen received, procedure incomplete"},		
        //    {"S", "No results available; procedure scheduled, but not done"},		
        //    {"A", "Some, but not all, results available"},		
        //    {"P", "Preliminary: A verified early result is available, final results not yet obtained"},		
        //    {"C", "Correction to results"},		
        //    {"R", "Results stored; not yet verified"},		
        //    {"F", "Final results; results stored and verified. Can only be changed with a corrected result."},		
        //    {"X", "No results available; Order canceled."},		
        //    {"Y", "No order on record for this test. (Used only on queries)"},		
        //    {"Z", "No record of this patient. (Used only on queries)"}	
        //};

        //#endregion

        //#region Order Status Conversion Table

        //static IDictionary<string, string> orderStatusConversionTable = new Dictionary<string, string>()
        //{
        //    {"A", "Some, but not all, results available"},		
        //    {"CA", "Order was canceled"},		
        //    {"CM", "Order is completed"},		
        //    {"DC", "Order was discontinued"},		
        //    {"ER", "Error, order not found"},		
        //    {"HD", "Order is on hold"},		
        //    {"IP", "In process, unspecified"},	
        //    {"RP", "Order has been replaced"},		
        //    {"SC", "In process, scheduled"}		
        //};

        //#endregion

        //#region Order Control Conversion Table

        //static IDictionary<string, string> orderControlConversionTable = new Dictionary<string, string>()
        //{
        //    {"NW", "New order"},
        //    {"OK", "Order accepted & OK"},
        //    {"UA", "Unable to Accept Order"},

        //    {"CA", "Cancel order request"},
        //    {"OC", "Order canceled"},
        //    {"CR", "Canceled as requested"},
        //    {"UC", "Unable to cancel"},

        //    {"DC", "Discontinue order request"},
        //    {"OD", "Order discontinued"},
        //    {"DR", "Discontinued as requested"},
        //    {"UD", "Unable to discontinue"},

        //    {"HD", "Hold order request"},
        //    {"OH", "Order held"},
        //    {"UH", "Unable to put on hold"},
        //    {"HR", "On hold as requested"},

        //    {"RL", "Release previous hold"},
        //    {"oe", "Order released"},
        //    {"OR", "Released as requested"},
        //    {"UR", "Unable to release"},

        //    {"RP", "Order replace request"},
        //    {"RU", "Replaced unsolicited"},
        //    {"RO", "Replacement order"},
        //    {"RQ", "Replaced as requested"},
        //    {"UM", "Unable to replace"},

        //    {"PA", "Parent order"},
        //    {"CH", "Child order"},

        //    {"XO", "Change order request"},
        //    {"XX", "Order changed, unsol."},
        //    {"UX", "Unable to change"},
        //    {"XR", "Changed as requested"},

        //    {"DE", "Data errors"},
        //    {"RE", "Observations to follow"},
        //    {"RR", "Request received"},
        //    {"SR", "Response to send order status request"},
        //    {"SS", "Send order status request"},
        //    {"SC", "Status changed"},
        //    {"SN", "Send order number"},
        //    {"NA", "Number assigned"},
        //    {"CN", "Combined result"},

        //    {"RF", "Refill order request"},
        //    {"AF", "Order refill request approval"},
        //    {"DF", "Order refill request denied"},
        //    {"FU", "Order refilled, unsolicited"},
        //    {"OF", "Order refilled as requested"},
        //    {"UF", "Unable to refill"},
        //    {"LI", "Link order to patient care message"},
        //    {"UN", "Unlink order from patient care message"}
        //};

        //#endregion

        #endregion
    }
}
