using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OruLettersService.Dto
{
    [Serializable]
    public class Letter
    {
        public string PatientId { get; set; }
        public string VisitId { get; set; }

        public string FillerOrderNumber { get; set; }
        public string FillerNamespace { get; set; }
        public string UniversalServiceId { get; set; }
        public string UniversalServiceText { get; set; }
        public DateTime? ObservationDate { get; set; }
        public string OrderingProviderId { get; set; }
        public string DiagnosticServiceSectionId { get; set; }

        public string ValueType { get; set; }
        public string ObservationId { get; set; }
        public string ObservationText { get; set; }
        public string ObservationValue { get; set; }
        public string AbnormalFlags { get; set; }
        public string ResultStatus { get; set; }
    }
}
