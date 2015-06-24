using System;
using Adt.Json;
using Newtonsoft.Json;

namespace Adt
{
    [Serializable]
    public class Insurance
    {
        public string PlanId { get; set; }
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public Address CompanyAddress { get; set; }
        public string CompanyPhone { get; set; }
        [JsonConverter(typeof(IsoDateConverter))]
        public DateTime? StartDate { get; set; }
        [JsonConverter(typeof(IsoDateConverter))]
        public DateTime? EndDate { get; set; }
        public string PlanType { get; set; }
        public string InsuredFirstName { get; set; }
        public string InsuredLastName { get; set; }
        public string InsuredRelationToPatient { get; set; }
        public Address InsuredAddress { get; set; }
        public Address InsuredEmployerAddress { get; set; }
        public string KG1 { get; set; }
        public string KG2 { get; set; }
        public string InsuredId { get; set; }
    }
}
