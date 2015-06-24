using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OruLabo
{
    [Serializable]
    public class ObservationResult
    {
        public string Type { get; set; }
        //public string ValueTypeDescription { get; set; }
        public string Id { get; set; }
        public string Text { get; set; }
        public string CodingSystem { get; set; }
        public string Value { get; set; }
        public string Units { get; set; }
        //public string UnitsText { get; set; }
        //public string UnitsCodingSystem { get; set; }
        public string ReferencesRange { get; set; }
        public string AbnormalFlags { get; set; }
        //public string AbnormalFlagsDescription { get; set; }
        public string ResultStatus { get; set; }
        //public string ResultStatusDescription { get; set; }
        public DateTime? Date { get; set; }
        public string ProducersId { get; set; }
    }
}
