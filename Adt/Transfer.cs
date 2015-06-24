using System;

namespace Adt
{
    [Serializable]
    public class Transfer
    {
        public DateTime? FromDate { get; set; }
        public bool? Pending { get; set; }
        public string CampusId { get; set; }
        public string WardId { get; set; }
        public string RoomId { get; set; }
        public string BedId { get; set; }
        public string PriorCampusId { get; set; }
        public string PriorWardId { get; set; }
        public string PriorRoomId { get; set; }
        public string PriorBedId { get; set; }
        public string TemporaryLocation { get; set; }
        public string LocationStatus { get; set; }
        public string PriorLocationStatus { get; set; }
        public string ConsultingDoctor { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentType { get; set; }
        public string FinancialClass { get; set; }
        public DateTime? FinancialClassFromDate { get; set; }
        public string RoomAsked { get; set; }
        public string RoomAssigned { get; set; }
    }
}
