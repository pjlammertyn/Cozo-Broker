using System;
using Adt.Json;
using Newtonsoft.Json;

namespace Adt
{
    [Serializable]
    public class Bed : Transfer
    {
        public Bed()
        {
        }

        public Bed(Transfer transfer, Patient patient, Visit visit)
        {
            FromDate = transfer.FromDate;
            Pending = transfer.Pending;
            CampusId = transfer.CampusId;
            WardId = transfer.WardId;
            RoomId = transfer.RoomId;
            BedId = transfer.BedId;
            PriorCampusId = transfer.PriorCampusId;
            PriorWardId = transfer.PriorWardId;
            PriorRoomId = transfer.PriorRoomId;
            PriorBedId = transfer.PriorBedId;
            TemporaryLocation = transfer.TemporaryLocation;
            LocationStatus = transfer.LocationStatus;
            PriorLocationStatus = transfer.PriorLocationStatus;
            ConsultingDoctor = transfer.ConsultingDoctor;
            DepartmentId = transfer.DepartmentId;
            DepartmentType = transfer.DepartmentType;
            FinancialClass = transfer.FinancialClass;
            FinancialClassFromDate = transfer.FinancialClassFromDate;
            RoomAsked = transfer.RoomAsked;
            RoomAssigned = transfer.RoomAssigned;

            PatientId = patient.Id;
            PatientLastName = patient.LastName;
            PatientFirstName = patient.FirstName;
            PatientBirthDate = patient.BirthDate;
            PatientSex = patient.Sex;

            VisitId = visit.Id;
            PendingAdmission = visit.Pending;
            PatientClass = visit.PatientClass;
            AdmissionType = visit.AdmissionType;
            AdmissionDate = visit.AdmissionDate;
            DischargeDate = visit.DischargeDate;
            PendingDischarge = visit.PendingDischarge;
        }

        public string PatientId { get; set; }
        public string PatientLastName { get; set; }
        public string PatientFirstName { get; set; }
        [JsonConverter(typeof(IsoDateConverter))]
        public DateTime? PatientBirthDate { get; set; }
        public string PatientSex { get; set; }

        public string VisitId { get; set; }
        public bool? PendingAdmission { get; set; }
        public string PatientClass { get; set; }
        public string AdmissionType { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public bool? PendingDischarge { get; set; }

        public string ConsultingDoctorLastName { get; set; }
        public string ConsultingDoctorFirstName { get; set; }
    }
}
