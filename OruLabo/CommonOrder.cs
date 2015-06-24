using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OruLabo
{
    [Serializable]
    public class CommonOrder
    {
        public string PatientId { get; set; }
        public string VisitId { get; set; }

        public string OrderControl { get; set; }
        //public string OrderControlDescription { get; set; }
        public string PlacerOrderNumber { get; set; }
        public string FillerOrderNumber { get; set; }
        public string OrderStatus { get; set; }
        //public string OrderStatusDescription { get; set; }
        public DateTime? StartTiming { get; set; }
        public DateTime? TransactionDateTime { get; set; }
        public string OrderingProviderId { get; set; }
        public string EnterersLocationPointOfCare { get; set; }
        public string EnteringOrganizationId { get; set; }

        public IList<ObservationRequest> Requests { get; set; }
    }
}
