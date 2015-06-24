using System;
using System.Collections.Generic;
using Adt.Json;
using Newtonsoft.Json;

namespace Adt
{
    [Serializable]
    public class Patient
    {
        public string Id { get; set; }
        public string MergedWithId { get; set; }
        public string ExternalId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Title { get; set; }
        public string PartnerLastName { get; set; }
        public string PartnerFirstName { get; set; }
        [JsonConverter(typeof(IsoDateConverter))]
        public DateTime? BirthDate { get; set; }
        public string Sex { get; set; }
        public Address Address { get; set; }
        public IList<string> Phones { get; set; }
        public IList<string> MobilePhones { get; set; }
        public IList<string> Faxes { get; set; }
        public IList<string> Emails { get; set; }
        public string Language { get; set; }
        public string SpokenLanguage { get; set; }
        public string MaritalStatus { get; set; }
        public string SSNNumber { get; set; }
        public string MotherId { get; set; }
        public string Nationality { get; set; }
        [JsonConverter(typeof(IsoDateConverter))]
        public DateTime? DeathDate { get; set; }
        public string HomeDoctorId { get; set; }
        public bool? HomeDoctorRecievesLetter { get; set; }
        public string HospitalRelation { get; set; }

        public IList<NextOfKin> NextOfKins { get; set; }
        public IList<Insurance> Insurances { get; set; }
        public IList<Observation> Observations { get; set; }
        public IList<Allergy> Allergies { get; set; }
    }
}
