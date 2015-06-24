using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OruLabo
{
    [Serializable]
    public class ObservationRequest
    {
        public string UniversalServiceId { get; set; }
        public string SpecimenSourceCode  { get; set; }
        public string SpecimenSourceName { get; set; }
        public string ResultStatus { get; set; }
        //public string ResultStatusDescription { get; set; }

        public IList<ObservationResult> Results { get; set; }
    }
}
