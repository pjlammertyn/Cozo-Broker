using System;
using Adt.Json;
using Newtonsoft.Json;

namespace Adt
{
    [Serializable]
    public class Reservation
    {
        public string PatientId { get; set; }
        public string PatientLastName { get; set; }
        public string PatientFirstName { get; set; }
        [JsonConverter(typeof(IsoDateConverter))]
        public DateTime? PatientBirthDate { get; set; }
        public string PatientSex { get; set; }

        public string VisitId { get; set; }
    }
}
