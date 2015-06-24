using System;
using System.Collections.Generic;

namespace Adt
{
    [Serializable]
    public class Visit
    {
        public string Id { get; set; }
        public bool? Pending { get; set; }
        public string PatientId { get; set; }
        public string PatientClass { get; set; }
        public string AdmissionType { get; set; }
        public string PreadmitNumber { get; set; }
        public string HomeDoctor { get; set; }
        public string ReferringDoctor { get; set; }
        public string TemporaryLocation { get; set; }
        public string AdmittingDoctor { get; set; }
        public string ChargePriceIndicator { get; set; }
        public bool? HomeDoctorRecievesLetter { get; set; }
        public bool? Internet { get; set; }
        public string DischargeDisposition { get; set; }
        public string DischargeToLocation { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public bool? PendingDischarge { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string ExternalId { get; set; }
        public DateTime? ExpectedAdmissionDate { get; set; }
        public DateTime? ExpectedDischargeDate { get; set; }
        public int? ExpectedAdmissionDays { get; set; }
        public string ReferringDoctor2 { get; set; }
        public string VisitPublicityCode { get; set; }
        public string ChargeAdjustmentCode { get; set; }
        public bool? NewbornBaby { get; set; }
        public bool? BabyDetained { get; set; }
        public string MKG { get; set; }

        public IList<Transfer> Transfers { get; set; }
        public IList<Observation> Observations { get; set; }
    }
}
