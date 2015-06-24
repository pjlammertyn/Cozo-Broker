using System;
using System.Collections.Generic;
using Adt.Json;
using Newtonsoft.Json;

namespace Adt
{
    [Serializable]
    public class Doctor
    {
        public string Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string HospitalRelation { get; set; }
        public string Sex { get; set; }
        [JsonConverter(typeof(IsoDateConverter))]
        public DateTime? BirthDate { get; set; }
        public bool? Active { get; set; }
        public IList<string> Phones { get; set; }
        public IList<string> MobilePhones { get; set; }
        public IList<string> Faxes { get; set; }
        public IList<string> Emails { get; set; }
        public Address Address { get; set; }
        public string PreferredContactMethod { get; set; }
        public string PreferredContactSubMethod { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string RizivNr { get; set; }
    }
}
