using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Adt;
using AsyncPoco;
using HL7ServiceBase;
using log4net;
using Newtonsoft.Json.Linq;
using RestClient;

namespace AdtService
{
    class HL7Parser : HL7ParserBase
    {
        #region Fields

        static readonly ILog log = LogManager.GetLogger(typeof(HL7Parser));

        static readonly string patientUri;
        static readonly string photoUri;
        static readonly string photoEidUri;
        static readonly string visitUri;
        static readonly string wardUri;
        static readonly string doctorUri;
        static readonly string insuranceCompanyUri;
        static readonly string visitsByPatientIdUri;
        static readonly bool deleteEmptyWards;
        static readonly IEnumerable<string> skippedDepartments;
        static IList<string> wardIds;


        string messageControlId;
        string messageType;
        string eventType;
        DateTime? messageDateTime;
        DateTime? eventOccured;
        string masterFileId;
        bool? masterFileDelete;
        Patient patient;
        Visit visit;
        Transfer transfer;
        Doctor doctor;
        InsuranceCompany insuranceCompany;
        Ward assignedWard;
        Ward priorWard;
        Bed bed;
        string consultingDoctorLastName;
        string consultingDoctorFirstName;

        #endregion

        #region Constructor

        static HL7Parser()
        {
            patientUri = ConfigurationManager.AppSettings["PatientUri"];
            photoUri = ConfigurationManager.AppSettings["PhotoUri"];
            photoEidUri = ConfigurationManager.AppSettings["PhotoEidUri"];
            visitUri = ConfigurationManager.AppSettings["VisitUri"];
            wardUri = ConfigurationManager.AppSettings["WardUri"];
            doctorUri = ConfigurationManager.AppSettings["DoctorUri"];
            insuranceCompanyUri = ConfigurationManager.AppSettings["InsuranceCompanyUri"];
            visitsByPatientIdUri = ConfigurationManager.AppSettings["VisitsByPatientIdUri"];

            deleteEmptyWards = ConfigurationManager.AppSettings["DeleteEmptyWards"].ToBool();
            skippedDepartments = ConfigurationManager.AppSettings["SkippedDepartments"].Split(',').ToList();

            wardIds = new List<string>();
        }

        public HL7Parser(Stream stream, GenericRestCrudHttpClient httpClient)
            : base(stream, httpClient)
        {
        }

        public HL7Parser(GenericRestCrudHttpClient httpClient)
            : base(httpClient)
        {
        }

        #endregion

        #region HL7ParserBase implementation

        protected override async Task ProcessSegment(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            var segmentId = line.Substring(0, line.IndexOf(fieldSep));
            switch (segmentId)
            {
                case "MSH":
                    ProcessMSH(line);
                    break;
                case "EVN":
                    ProcessEVN(line);
                    break;
                case "PID":
                    await ProcessPID(line).ConfigureAwait(false);
                    break;
                case "PD1":
                    ProcessPD1(line);
                    break;
                case "NK1":
                    ProcessNK1(line);
                    break;
                case "IN1":
                    ProcessIN1(line);
                    break;
                case "OBX":
                    await ProcessOBX(line).ConfigureAwait(false);
                    break;
                case "PV1":
                    await ProcessPV1(line).ConfigureAwait(false);
                    break;
                case "PV2":
                    ProcessPV2(line);
                    break;
                case "AL1":
                    ProcessAL1(line);
                    break;
                case "MRG":
                    await ProcessMRG(line).ConfigureAwait(false);
                    break;
                case "MFI":
                    ProcessMFI(line);
                    break;
                case "MFE":
                    await ProcessMFE(line).ConfigureAwait(false);
                    break;
                case "STF":
                    ProcessSTF(line);
                    break;
                case "PRA":
                    ProcessPRA(line);
                    break;
                default:
                    break;
            }
        }

        protected override async Task StoreDocuments()
        {
            if (patient != null)
                await httpClient.PutAsync(string.Format(patientUri, patient.Id), patient).ConfigureAwait(false);
            if (visit != null)
            {
                if (transfer != null)
                    await ProcessBedOccupation().ConfigureAwait(false);
                await ProcessVisit().ConfigureAwait(false);
            }
            if (doctor != null)
            {
                if (masterFileDelete.GetValueOrDefault())
                    await httpClient.DeleteAsync(string.Format(doctorUri, doctor.Id)).ConfigureAwait(false);
                else
                    await httpClient.PutAsync(string.Format(doctorUri, doctor.Id), doctor).ConfigureAwait(false);
            }
            if (insuranceCompany != null)
            {
                if (masterFileDelete.GetValueOrDefault())
                    await httpClient.DeleteAsync(string.Format(insuranceCompanyUri, insuranceCompany.Id)).ConfigureAwait(false);
                else
                    await httpClient.PutAsync(string.Format(insuranceCompanyUri, insuranceCompany.Id), insuranceCompany).ConfigureAwait(false);
            }
        }

        #endregion

        #region Methods

        async Task ProcessBedOccupation()
        {
            if (!string.IsNullOrEmpty(transfer.DepartmentId) && skippedDepartments.Contains(transfer.DepartmentId))
                return; //DO NOT STORE BED OCCUPATION FOR SKIPPED DEPARTMENTS

            bed = new Bed(transfer, patient, visit)
            {
                ConsultingDoctorLastName = consultingDoctorLastName,
                ConsultingDoctorFirstName = consultingDoctorFirstName
            };

            if (!string.IsNullOrEmpty(transfer.WardId))
                assignedWard = await GetWard(transfer.CampusId, transfer.WardId).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(transfer.PriorWardId))
            {
                if (transfer.WardId == transfer.PriorWardId)
                    priorWard = assignedWard;
                else
                    priorWard = await GetWard(transfer.PriorCampusId, transfer.PriorWardId).ConfigureAwait(false);
            }

            switch (eventType)
            {
                case "A01": //Admit a patient   
                case "A04": //Register a patient
                case "A14": //Pending admit
                    await RemoveVisitFromAllWard();
                    AddBedToWard(bed, assignedWard);
                    break;
                case "A02": //Transfer a patient
                    if (visit.DischargeDate.HasValue && !visit.PendingDischarge.GetValueOrDefault() && visit.DischargeDate.Value > bed.FromDate.GetValueOrDefault(DateTime.Now))
                        break; //transfer before dichargedate
                    if (visit.DischargeDate.HasValue && !visit.PendingDischarge.GetValueOrDefault() && visit.DischargeDate.Value < bed.FromDate.GetValueOrDefault(DateTime.Now).AddMonths(-1))
                        break; //discharged more than 1 month ago
                    if (visit.Transfers != null)
                    {
                        var lastFromDate = (from t in visit.Transfers
                                            where t.FromDate < DateTime.Now && !t.Pending.GetValueOrDefault()
                                            orderby t.FromDate descending
                                            select t.FromDate).FirstOrDefault();
                        if (bed.FromDate < lastFromDate)
                        {
                            assignedWard = null; // transfer in the past
                            break;
                        }
                    }

                    if (bed.LocationStatus != "1" && bed.LocationStatus != "2" && visit.Transfers != null) //back to original bed!!
                    {
                        foreach (var t in visit.Transfers)
                        {
                            if (string.IsNullOrEmpty(t.WardId) || t.Pending.GetValueOrDefault() || t.FromDate > DateTime.Now)
                                continue;
                            var ward = await httpClient.GetAsync<Ward>(string.Format(wardUri, string.Concat(t.CampusId, "-", t.WardId))).ConfigureAwait(false);
                            if (ward != null)
                            {
                                RemoveBedFromWard(t, ward, checkFromDate: false, removePendingDischarge: false);
                                await StoreWard(ward).ConfigureAwait(false);
                            }
                        }
                        priorWard = null;
                    }

                    if (!visit.PendingDischarge.GetValueOrDefault() && visit.DischargeDate.HasValue && visit.DischargeDate <= DateTime.Now)
                        assignedWard = null;

                    AddBedToWard(bed, assignedWard);
                    break;
                case "A15": //Pending transfer
                    AddBedToWard(bed, assignedWard);
                    break;
                case "A03": //Discharge a patient  
                    await RemoveVisitFromAllWard();
                    RemoveBedFromWard(transfer, assignedWard, checkFromDate: false);
                    break;
                case "A05": //Preadmit a patient
                    break;
                case "A08": //Update patient information
                    break;
                case "A11": //Cancel admit
                case "A27": //Cancel pending admit
                    RemoveBedFromWard(transfer, assignedWard, checkFromDate: false);
                    break;
                case "A12": //Cancel transfer
                    if (visit.DischargeDate.HasValue && !visit.PendingDischarge.GetValueOrDefault() && visit.DischargeDate.Value > bed.FromDate.GetValueOrDefault(DateTime.Now))
                        break; //transfer before dichargedate
                    if (visit.DischargeDate.HasValue && !visit.PendingDischarge.GetValueOrDefault() && visit.DischargeDate.Value < bed.FromDate.GetValueOrDefault(DateTime.Now).AddMonths(-1))
                        break; //discharged more than 1 month ago
                    RemoveBedFromWard(transfer, assignedWard);
                    if (visit.Transfers != null)
                    {
                        var lastFromDate = (from t in visit.Transfers
                                            where t.FromDate < DateTime.Now
                                            orderby t.FromDate descending
                                            select t.FromDate).FirstOrDefault();
                        if (bed.FromDate < lastFromDate)
                        {
                            assignedWard = null; // transfer in the past
                            break;
                        }
                    }
                    if (!visit.PendingDischarge.GetValueOrDefault() && visit.DischargeDate.HasValue && visit.DischargeDate <= DateTime.Now)
                    { //discharged in the past
                        assignedWard = null;
                        break;
                    }
                    if (visit.Transfers != null && visit.Transfers.Count > 1)
                    { //more than 1 transferline
                        bed.CampusId = bed.PriorCampusId;
                        bed.WardId = bed.PriorWardId;
                        bed.RoomId = bed.PriorRoomId;
                        bed.BedId = bed.PriorBedId;
                        bed.LocationStatus = bed.PriorLocationStatus;
                        bed.PriorCampusId = null;
                        bed.PriorWardId = null;
                        bed.PriorRoomId = null;
                        bed.PriorBedId = null;
                        bed.PriorLocationStatus = null;
                        bed.FromDate = (from t in visit.Transfers
                                        where t.FromDate < bed.FromDate
                                        orderby t.FromDate descending
                                        select t.FromDate).FirstOrDefault() ?? bed.FromDate;
                        AddBedToWard(bed, priorWard);
                    }
                    break;
                case "A26": //Cancel pending transfer
                    RemoveBedFromWard(transfer, assignedWard);
                    //check if next visit is pending discharge and has the same bed
                    // THAN change bed to previous bed
                    break;
                case "A13": //Cancel discharge
                    if (visit.Transfers != null)
                    {
                        var lastFromDate = (from t in visit.Transfers
                                            where t.FromDate < bed.FromDate
                                            orderby t.FromDate descending
                                            select t.FromDate).FirstOrDefault();
                        if (lastFromDate.HasValue)
                            bed.FromDate = lastFromDate;

                        AddBedToWard(bed, assignedWard);
                    }
                    bed.DischargeDate = null;
                    bed.PendingDischarge = null;
                    break;
                case "A25": //Cancel pending discharge
                    await RemoveDischargeDateFromAllBeds(visit.Id);
                    RemoveBedFromWard(transfer, assignedWard, checkLocation: false);
                    break;
                case "A16": //Pending discharge 
                    bed.PendingDischarge = true;
                    AddBedToWard(bed, assignedWard);
                    break;
                case "A24": //Link Patient Information
                    break;
                case "A28": //Add person information
                    break;
                case "A31": //Update person information
                    break;
                case "A37": //Unlink Patient Information
                    break;
                case "A38": //Cancel preadmit
                    break;
                case "A40": //Merge patient
                    break;
                default:
                    break;
            }

            await StoreWard(assignedWard);

            if (priorWard != null && assignedWard != priorWard)
                await StoreWard(priorWard);
        }

        void AddBedToWard(Bed bed, Ward ward)
        {
            if (ward != null && ward.Beds.FindIndex(x => x.PatientId == bed.PatientId && x.VisitId == bed.VisitId && x.FromDate == bed.FromDate && x.CampusId == bed.CampusId && x.WardId == bed.WardId && x.RoomId == bed.RoomId && x.BedId == bed.BedId) == -1)
            {
                if (log.IsInfoEnabled)
                    log.InfoFormat("Ward {0}: added bed {1}-{2}-{3}-{4} fromDate '{5}' for visit {6}", ward.Id, bed.CampusId, bed.WardId, bed.RoomId, bed.BedId, bed.FromDate.Maybe(d => d.Value.ToString("yyyy-MM-dd HH:mm")), bed.VisitId);
                ward.Beds.Add(bed);
            }
        }

        void RemoveBedFromWard(Transfer transfer, Ward ward, bool checkFromDate = true, bool checkLocation = true, bool removePendingDischarge = true)
        {
            if (ward == null || ward.Beds == null || !ward.Beds.Any())
                return;

            var bedsToRemove = (from b in ward.Beds
                                where b.VisitId == visit.Id &&
                                    b.PatientId == patient.Id &&
                                    (checkFromDate ? b.FromDate == transfer.FromDate : true) &&
                                    (checkLocation ?
                                        (b.CampusId == transfer.CampusId &&
                                        b.WardId == transfer.WardId &&
                                        b.RoomId == transfer.RoomId &&
                                        b.BedId == transfer.BedId) : true) &&
                                    (removePendingDischarge ? true : !b.PendingDischarge.GetValueOrDefault())
                                select b).ToList();

            foreach (var item in bedsToRemove)
            {
                if (log.IsInfoEnabled)
                    log.InfoFormat("Ward {0}: removed bed {1}-{2}-{3}-{4} fromDate '{5}' for visit {6}", ward.Id, item.CampusId, item.WardId, item.RoomId, item.BedId, item.FromDate.Maybe(d => d.Value.ToString("yyyy-MM-dd HH:mm")), item.VisitId);
                ward.Beds.Remove(item);
            }
        }

        async Task RemoveDischargeDateFromAllBeds(string visitId)
        {
            foreach (var wardId in wardIds.ToArray()) //ToArray --> clone for thread safety
            {
                if (string.IsNullOrEmpty(wardId))
                    continue;
                var ward = await httpClient.GetAsync<Ward>(string.Format(wardUri, wardId)).ConfigureAwait(false);
                if (ward != null && ward.Beds != null)
                {
                    foreach (var bed in ward.Beds)
                    {
                        if (bed.VisitId != visitId)
                            continue;

                        bed.PendingDischarge = null;
                        bed.DischargeDate = null;
                    }
                    await StoreWard(ward).ConfigureAwait(false);
                }
            }
        }

        async Task RemoveVisitFromAllWard()
        {
            foreach (var wardId in wardIds.ToArray()) //ToArray --> clone for thread safety
            {
                if (string.IsNullOrEmpty(wardId))
                    continue;
                var ward = await httpClient.GetAsync<Ward>(string.Format(wardUri, wardId)).ConfigureAwait(false);
                if (ward != null)
                {
                    RemoveVisitFromWard(ward);
                    await StoreWard(ward).ConfigureAwait(false);
                }
            }
        }

        void RemoveVisitFromWard(Ward ward)
        {
            if (ward == null || ward.Beds == null || !ward.Beds.Any())
                return;

            var bedsToRemove = (from b in ward.Beds
                                where b.VisitId == visit.Id &&
                                    b.PatientId == patient.Id
                                select b).ToList();

            foreach (var item in bedsToRemove)
            {
                if (log.IsInfoEnabled)
                    log.InfoFormat("Ward {0}: removed bed {1}-{2}-{3}-{4} fromDate '{5}' for visit {6}", ward.Id, item.CampusId, item.WardId, item.RoomId, item.BedId, item.FromDate.Maybe(d => d.Value.ToString("yyyy-MM-dd HH:mm")), item.VisitId);
                ward.Beds.Remove(item);
            }
        }

        async Task StoreWard(Ward ward)
        {
            if (ward != null)
            {
                if (ward.Beds != null && !ward.Beds.Any())
                    ward.Beds = null;

                var id = string.Concat(ward.CampusId, "-", ward.Id);
                if (ward.Beds != null)
                {
                    ward.Beds = (from bed in ward.Beds 
                                 let orderbyKey = string.Concat(bed.RoomId, "-", bed.BedId, "-", bed.FromDate.GetValueOrDefault().ToString("yyyy-MM-dd HH:mm"))
                                     orderby orderbyKey
                                     select bed).ToList();
                    await httpClient.PutAsync(string.Format(wardUri, id), ward).ConfigureAwait(false);
                }
                else if (deleteEmptyWards)
                {
                    wardIds.Remove(id); //remove from wardIds cache
                    await httpClient.DeleteAsync(string.Format(wardUri, id)).ConfigureAwait(false);
                }
            }
        }

        async Task<Ward> GetWard(string campusId, string wardId)
        {
            var id = string.Concat(campusId, "-", wardId);
            var ward = await httpClient.GetAsync<Ward>(string.Format(wardUri, id)).ConfigureAwait(false);
            if (ward == null)
            {
                wardIds.Add(id); //add to wardIds cache
                ward = new Ward() { CampusId = campusId, Id = wardId };
            }
            if (ward.Beds == null)
                ward.Beds = new List<Bed>();

            if (string.IsNullOrEmpty(ward.Description))
                ward.Description = await GetWardDescription(campusId, wardId).ConfigureAwait(false);

            return ward;
        }

        async Task<string> GetWardDescription(string campusId, string wardId)
        {
            string description = null;
            try
            {
                using (var db = new Database("oazis"))
                {
                    description = await db.SingleOrDefaultAsync<string>(@"select wd.ward_descr
from oazp..ward_descr wd with (nolock)
where wd.campus_id = @0 and wd.ward_id = @1
and lkp_language = 'NL'
and from_date = 
(
	select max(wd1.from_date) 
	from oazp..ward_descr wd1 with (nolock)
	where lkp_language = 'NL'
	and wd1.campus_id = wd.campus_id and wd1.ward_id = wd.ward_id
)", campusId, wardId).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.Error(string.Format(""), ex);

            }
            return description.Maybe(s => s.Trim());
        }

        async Task ProcessVisit()
        {
            int transferIndex;

            if (visit.Transfers == null)
                visit.Transfers = new List<Transfer>();

            switch (eventType)
            {
                case "A01": //Admit a patient   
                case "A04": //Register a patient
                case "A14": //Pending admit
                    visit.Transfers = new List<Transfer>(); //clear all transfers
                    visit.Transfers.Add(transfer);
                    break;
                case "A02": //Transfer a patient
                    transferIndex = visit.Transfers.FindIndex(x => x.FromDate == eventOccured);
                    if (transferIndex != -1)
                        visit.Transfers.RemoveAt(transferIndex);
                    visit.Transfers.Add(transfer);
                    break;
                case "A03": //Discharge a patient
                    visit.PendingDischarge = null;
                    visit.DischargeDate = eventOccured;
                    transferIndex = visit.Transfers.FindIndex(x => x.FromDate == eventOccured);
                    if (transferIndex != -1)
                        visit.Transfers.RemoveAt(transferIndex);
                    visit.Transfers.Add(transfer);
                    break;
                case "A05": //Preadmit a patient
                    break;
                case "A08": //Update patient information
                    break;
                case "A11": //Cancel admit
                    await httpClient.DeleteAsync(string.Format(visitUri, visit.Id)).ConfigureAwait(false);
                    visit = null;
                    break;
                case "A12": //Cancel transfer
                    transferIndex = visit.Transfers.FindIndex(x => x.FromDate == eventOccured);
                    if (transferIndex != -1)
                        visit.Transfers.RemoveAt(transferIndex);
                    break;
                case "A13": //Cancel discharge
                    visit.DischargeDate = null;
                    visit.PendingDischarge = null;
                    transferIndex = visit.Transfers.FindIndex(x => x.FromDate == eventOccured);
                    if (transferIndex != -1)
                        visit.Transfers.RemoveAt(transferIndex);
                    break;
                case "A15": //Pending transfer
                    transferIndex = visit.Transfers.FindIndex(x => x.FromDate == eventOccured);
                    if (transferIndex != -1)
                        visit.Transfers.RemoveAt(transferIndex);
                    visit.Transfers.Add(transfer);
                    break;
                case "A16": //Pending discharge
                    visit.PendingDischarge = true;
                    transferIndex = visit.Transfers.FindIndex(x => x.FromDate == eventOccured);
                    if (transferIndex != -1)
                        visit.Transfers.RemoveAt(transferIndex);
                    visit.Transfers.Add(transfer);
                    break;
                case "A24": //Link Patient Information
                    break;
                case "A25": //Cancel pending discharge
                    visit.PendingDischarge = null;
                    visit.DischargeDate = null;
                    transferIndex = visit.Transfers.FindIndex(x => x.FromDate == eventOccured);
                    if (transferIndex != -1)
                        visit.Transfers.RemoveAt(transferIndex);
                    break;
                case "A26": //Cancel pending transfer
                    transferIndex = visit.Transfers.FindIndex(x => x.FromDate == eventOccured);
                    if (transferIndex != -1)
                        visit.Transfers.RemoveAt(transferIndex);
                    break;
                case "A27": //Cancel pending admit
                    await httpClient.DeleteAsync(string.Format(visitUri, visit.Id)).ConfigureAwait(false);
                    visit = null;
                    break;
                case "A28": //Add person information
                    break;
                case "A31": //Update person information
                    break;
                case "A37": //Unlink Patient Information
                    break;
                case "A38": //Cancel preadmit
                    break;
                case "A40": //Merge patient
                    break;
                default:
                    break;
            }

            if (visit != null)
            {
                await CheckDischargeTransferLocation().ConfigureAwait(false);

                await StoreVisit(visit).ConfigureAwait(false);
            }
        }

        async Task StoreVisit(Visit visit)
        {
            if (visit != null)
            {
                if (visit.Transfers != null && !visit.Transfers.Any())
                    visit.Transfers = null;
                else
                    visit.Transfers = (from transfer in visit.Transfers
                                       orderby transfer.FromDate.GetValueOrDefault(DateTime.MaxValue)
                                       select transfer).ToList();

                await httpClient.PutAsync(string.Format(visitUri, visit.Id), visit).ConfigureAwait(false);
            }
        }


        async Task CheckDischargeTransferLocation()
        {
            if (visit == null || visit.Transfers == null || visit.Transfers.Count <= 1)
                return;

            if (!visit.DischargeDate.HasValue)
                return;

            if (visit.DischargeDate != visit.Transfers.Last().FromDate)
                return;

            var lastTransfer = visit.Transfers[visit.Transfers.Count - 1];
            var secondLastTransfer = visit.Transfers[visit.Transfers.Count - 2];

            if (lastTransfer.CampusId == secondLastTransfer.CampusId &&
                lastTransfer.WardId == secondLastTransfer.WardId &&
                lastTransfer.RoomId == secondLastTransfer.RoomId &&
                lastTransfer.BedId == secondLastTransfer.BedId)
                return;

            var lastWard = await httpClient.GetAsync<Ward>(string.Format(wardUri, string.Concat(lastTransfer.CampusId, "-", lastTransfer.WardId))).ConfigureAwait(false);
            if (lastWard != null && lastWard.Beds != null)
            {
                var index = lastWard.Beds.FindIndex(x => x.PatientId == patient.Id && x.VisitId == visit.Id && x.FromDate == lastTransfer.FromDate && x.CampusId == lastTransfer.CampusId && x.WardId == lastTransfer.WardId && x.RoomId == lastTransfer.RoomId && x.BedId == lastTransfer.BedId);
                if (index != -1)
                {
                    var lastBed = lastWard.Beds.ElementAt(index);
                    lastWard.Beds.RemoveAt(index);
                    await StoreWard(lastWard).ConfigureAwait(false);

                    lastBed.CampusId = secondLastTransfer.CampusId;
                    lastBed.WardId = secondLastTransfer.WardId;
                    lastBed.RoomId = secondLastTransfer.RoomId;
                    lastBed.BedId = secondLastTransfer.BedId;
                    lastBed.PriorCampusId = secondLastTransfer.PriorCampusId;
                    lastBed.PriorWardId = secondLastTransfer.PriorWardId;
                    lastBed.PriorRoomId = secondLastTransfer.PriorRoomId;
                    lastBed.PriorBedId = secondLastTransfer.PriorBedId;

                    var secondLastWard = await GetWard(secondLastTransfer.CampusId, secondLastTransfer.WardId).ConfigureAwait(false);
                    secondLastWard.Beds.Add(lastBed);
                    await StoreWard(secondLastWard).ConfigureAwait(false);
                }
            }

            lastTransfer.CampusId = secondLastTransfer.CampusId;
            lastTransfer.WardId = secondLastTransfer.WardId;
            lastTransfer.RoomId = secondLastTransfer.RoomId;
            lastTransfer.BedId = secondLastTransfer.BedId;
        }

        #endregion

        #region HouseKeepingMethods

        public async Task FillWardCache()
        {
            if (log.IsInfoEnabled)
                log.InfoFormat("Fill ward cache");
            //SLOW QUERY!!!!!!!!!!
            var wardResult = await httpClient.GetAsync<Adt.CouchDb.Result<Ward>>(string.Format(wardUri, "_all_docs?include_docs=true")).ConfigureAwait(false);
            //SO WE CACHE RESULT!!!!!

            wardIds = (from row in wardResult.rows
                       where row.doc != null && !string.IsNullOrEmpty(row.doc.CampusId) && !string.IsNullOrEmpty(row.doc.Id)
                       select string.Concat(row.doc.CampusId, "-", row.doc.Id)).ToList();
            foreach (var wardId in wardIds.ToArray()) //ToArray --> clone for thread safety
            {
                if (string.IsNullOrEmpty(wardId))
                    continue;
                await httpClient.GetAsync<Ward>(string.Format(wardUri, wardId)).ConfigureAwait(false);
            }
        }

        public async Task RemovBedsFromWards()
        {
            if (log.IsInfoEnabled)
                log.InfoFormat("HOUSEKEEPING: remove discharged and pending admision beds from wards");

            foreach (var wardId in wardIds.ToArray()) //ToArray --> clone for thread safety
            {
                if (string.IsNullOrEmpty(wardId))
                    continue;
                var ward = await httpClient.GetAsync<Ward>(string.Format(wardUri, wardId)).ConfigureAwait(false);
                if (ward != null)
                {
                    var bedsToRemove = (from b in ward.Beds
                                        where /*b.PendingDischarge.GetValueOrDefault() &&*/ b.DischargeDate.GetValueOrDefault(DateTime.MaxValue) <= DateTime.Now.Date.AddDays(-3)
                                        select b).ToList();

                    foreach (var item in bedsToRemove)
                    {
                        if (log.IsInfoEnabled)
                            log.InfoFormat("Ward {0}: removed bed {1}-{2}-{3}-{4} fromDate '{5}' for visit {6}", ward.Id, item.CampusId, item.WardId, item.RoomId, item.BedId, item.FromDate.Maybe(d => d.Value.ToString("yyyy-MM-dd HH:mm")), item.VisitId);
                        ward.Beds.Remove(item);
                    }

                    bedsToRemove = (from b in ward.Beds
                                    where b.PendingAdmission.GetValueOrDefault() && b.AdmissionDate.GetValueOrDefault(DateTime.MaxValue) <= DateTime.Now.Date
                                    select b).ToList();

                    foreach (var item in bedsToRemove)
                    {
                        if (log.IsInfoEnabled)
                            log.InfoFormat("Ward {0}: removed bed {1}-{2}-{3}-{4} fromDate '{5}' for visit {6}", ward.Id, item.CampusId, item.WardId, item.RoomId, item.BedId, item.FromDate.Maybe(d => d.Value.ToString("yyyy-MM-dd HH:mm")), item.VisitId);
                        ward.Beds.Remove(item);
                    }

                    var visitIds = (from b in ward.Beds
                                    where !string.IsNullOrEmpty(b.VisitId)
                                    select b.VisitId).Distinct().ToList();
                    var visitResult = await httpClient.GetAsync<Adt.CouchDb.Result<Visit>>(string.Format(visitUri, "_all_docs?include_docs=true"), visitIds).ConfigureAwait(false);
                    var dischargedVisitIds = (from row in visitResult.rows
                                              where row.doc != null && row.doc.DischargeDate.GetValueOrDefault(DateTime.MaxValue) <= DateTime.Now.Date.AddDays(-3)
                                              select row.doc.Id).ToList();

                    bedsToRemove = (from b in ward.Beds
                                    where dischargedVisitIds.Contains(b.VisitId)
                                    select b).ToList();

                    foreach (var item in bedsToRemove)
                    {
                        if (log.IsInfoEnabled)
                            log.InfoFormat("Ward {0}: removed bed {1}-{2}-{3}-{4} fromDate '{5}' for visit {6}", ward.Id, item.CampusId, item.WardId, item.RoomId, item.BedId, item.FromDate.Maybe(d => d.Value.ToString("yyyy-MM-dd HH:mm")), item.VisitId);
                        ward.Beds.Remove(item);
                    }

                    await StoreWard(ward).ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region HL7 Segment Methods

        void ProcessMSH(string segment)
        {
            fieldSep = segment[3];
            componentSep = segment[4];
            repetitionSep = segment[5];
            escapeChar = segment[6];
            subComponentSep = segment[7];

            var fields = segment.Split(fieldSep);

            messageDateTime = GetValueFromField(fields.ElementAtOrDefault(6)).ToNullableDatetime("yyyyMMddHHmmss", "yyyyMMddHHmm");
            messageType = GetValueFromField(fields.ElementAtOrDefault(8));
            eventType = GetValueFromField(fields.ElementAtOrDefault(8), componentIndex: 1);
            messageControlId = GetValueFromField(fields.ElementAtOrDefault(9));
        }

        void ProcessEVN(string segment)
        {
            var fields = segment.Split(fieldSep);

            eventType = GetValueFromField(fields.ElementAtOrDefault(1));
            eventOccured = GetValueFromField(fields.ElementAtOrDefault(6)).ToNullableDatetime("yyyyMMddHHmmss", "yyyyMMddHHmm");
        }

        async Task ProcessPID(string segment)
        {
            var fields = segment.Split(fieldSep);

            var patientId = GetValueFromField(fields.ElementAtOrDefault(3)); //A99 P01 P05
            patient = await httpClient.GetAsync<Patient>(string.Format(patientUri, patientId)).ConfigureAwait(false);
            if (patient == null)
                patient = new Patient() { Id = patientId };

            patient.ExternalId = GetValueFromField(fields.ElementAtOrDefault(2));
            patient.LastName = GetValueFromField(fields.ElementAtOrDefault(5)); //A99 P01 P05
            patient.FirstName = GetValueFromField(fields.ElementAtOrDefault(5), componentIndex: 1); ////A99 P01 P05
            patient.Title = GetValueFromField(fields.ElementAtOrDefault(5), componentIndex: 4); ////A99 P01 P05
            patient.PartnerLastName = GetValueFromField(fields.ElementAtOrDefault(6)); //A99 P01 P05
            patient.PartnerFirstName = GetValueFromField(fields.ElementAtOrDefault(6), componentIndex: 1); //A99 P01 P05
            patient.BirthDate = GetValueFromField(fields.ElementAtOrDefault(7)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmmss", "dd/MM/yyyy"); //A99 P01 P05
            if ((new string[] { "A01", "A04", "A05", "A08", "A14" }).Contains(eventType))
                patient.Sex = GetValueFromField(fields.ElementAtOrDefault(8));
            if ((new string[] { "A01", "A04", "A05", "A08", "A14", "A28", "A31", "A14" }).Contains(eventType))
            {
                var street = GetValueFromField(fields.ElementAtOrDefault(11));
                var city = GetValueFromField(fields.ElementAtOrDefault(11), componentIndex: 2);
                var postalCode = GetValueFromField(fields.ElementAtOrDefault(11), componentIndex: 4);
                var countryCode = GetValueFromField(fields.ElementAtOrDefault(11), componentIndex: 5);
                if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(postalCode) || !string.IsNullOrEmpty(countryCode))
                    patient.Address = new Address() { Street = street, City = city, PostalCode = postalCode, CountryCode = countryCode };
            }
            if ((new string[] { "A01", "A04", "A05", "A08", "A14", "A28", "A31" }).Contains(eventType))
            {
                patient.Phones = null;
                patient.MobilePhones = null;
                patient.Faxes = null;
                patient.Emails = null;
                foreach (var repetition in fields.ElementAtOrDefault(13).Split(repetitionSep))
                {
                    switch (GetValueFromField(repetition, componentIndex: 2))
                    {
                        case "PH":
                            if (patient.Phones == null)
                                patient.Phones = new List<string>();
                            patient.Phones.Add(GetValueFromField(repetition));
                            break;
                        case "CP":
                            if (patient.MobilePhones == null)
                                patient.MobilePhones = new List<string>();
                            patient.MobilePhones.Add(GetValueFromField(repetition));
                            break;
                        case "FX":
                            if (patient.Faxes == null)
                                patient.Faxes = new List<string>();
                            patient.Faxes.Add(GetValueFromField(repetition));
                            break;
                    }

                    var email = GetValueFromField(repetition, componentIndex: 3);
                    if (!string.IsNullOrEmpty(email))
                    {
                        if (patient.Emails == null)
                            patient.Emails = new List<string>();
                        patient.Emails.Add(email);
                    }
                }
            }
            patient.Language = GetValueFromField(fields.ElementAtOrDefault(15)); //A99
            if ((new string[] { "A28", "A31" }).Contains(eventType))
                patient.SpokenLanguage = GetValueFromField(fields.ElementAtOrDefault(15), componentIndex: 4);
            if ((new string[] { "A01", "A04", "A05", "A08" }).Contains(eventType))
                patient.MaritalStatus = GetValueFromField(fields.ElementAtOrDefault(16));
            if ((new string[] { "A01", "A04", "A05", "A08" }).Contains(eventType))
                patient.SSNNumber = GetValueFromField(fields.ElementAtOrDefault(20));
            if ((new string[] { "A01", "A04", "A05", "A08" }).Contains(eventType))
                patient.MotherId = GetValueFromField(fields.ElementAtOrDefault(21));
            if ((new string[] { "A01", "A04", "A05", "A08" }).Contains(eventType))
                patient.Nationality = GetValueFromField(fields.ElementAtOrDefault(26));
            patient.DeathDate = GetValueFromField(fields.ElementAtOrDefault(29)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmmss", "dd/MM/yyyy"); //A99
        }

        void ProcessPD1(string segment)
        {
            var fields = segment.Split(fieldSep);

            patient.HomeDoctorId = GetValueFromField(fields.ElementAtOrDefault(4)); //A99
            patient.HomeDoctorRecievesLetter = GetValueFromField(fields.ElementAtOrDefault(12)).Maybe(x => x == "N" ? (bool?)true : null); //A99
            patient.HospitalRelation = GetValueFromField(fields.ElementAtOrDefault(15)); //A99
        }

        void ProcessNK1(string segment)
        {
            var fields = segment.Split(fieldSep);

            var nextOfKin = new NextOfKin();

            var index = GetValueFromField(fields.ElementAtOrDefault(1));
            if (index == "1")
                patient.NextOfKins = null;

            nextOfKin.Name = GetValueFromField(fields.ElementAtOrDefault(2));
            nextOfKin.Description = GetValueFromField(fields.ElementAtOrDefault(3), componentIndex: 1);
            var street = GetValueFromField(fields.ElementAtOrDefault(4));
            var city = GetValueFromField(fields.ElementAtOrDefault(4), componentIndex: 2);
            var postalCode = GetValueFromField(fields.ElementAtOrDefault(4), componentIndex: 4);
            var countryCode = GetValueFromField(fields.ElementAtOrDefault(4), componentIndex: 5);
            if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(postalCode) || !string.IsNullOrEmpty(countryCode))
                nextOfKin.Address = new Address() { Street = street, City = city, PostalCode = postalCode, CountryCode = countryCode };
            nextOfKin.Phones = null;
            nextOfKin.MobilePhones = null;
            nextOfKin.Faxes = null;
            nextOfKin.Emails = null;
            foreach (var repetition in fields.ElementAtOrDefault(5).Split(repetitionSep))
            {
                switch (GetValueFromField(repetition, componentIndex: 2))
                {
                    case "PH":
                        if (nextOfKin.Phones == null)
                            nextOfKin.Phones = new List<string>();
                        nextOfKin.Phones.Add(GetValueFromField(repetition));
                        break;
                    case "CP":
                        if (nextOfKin.MobilePhones == null)
                            nextOfKin.MobilePhones = new List<string>();
                        nextOfKin.MobilePhones.Add(GetValueFromField(repetition));
                        break;
                    case "FX":
                        if (nextOfKin.Faxes == null)
                            nextOfKin.Faxes = new List<string>();
                        nextOfKin.Faxes.Add(GetValueFromField(repetition));
                        break;
                }

                var email = GetValueFromField(repetition, componentIndex: 3);
                if (!string.IsNullOrEmpty(email))
                {
                    if (nextOfKin.Emails == null)
                        nextOfKin.Emails = new List<string>();
                    nextOfKin.Emails.Add(email);
                }
            }
            nextOfKin.Type = GetValueFromField(fields.ElementAtOrDefault(7));
            nextOfKin.Index = GetValueFromField(fields.ElementAtOrDefault(7), componentIndex: 1);
            nextOfKin.FromDate = GetValueFromField(fields.ElementAtOrDefault(8)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmmss", "dd/MM/yyyy");
            nextOfKin.Language = GetValueFromField(fields.ElementAtOrDefault(20));

            if (patient.NextOfKins == null)
                patient.NextOfKins = new List<NextOfKin>();
            patient.NextOfKins.Add(nextOfKin);
        }

        void ProcessIN1(string segment)
        {
            var fields = segment.Split(fieldSep);

            if (messageType == "ADT")
            {
                var insurance = new Insurance();

                var index = GetValueFromField(fields.ElementAtOrDefault(1));
                if (index == "1")
                    patient.Insurances = null;

                insurance.PlanId = GetValueFromField(fields.ElementAtOrDefault(2));
                insurance.CompanyId = GetValueFromField(fields.ElementAtOrDefault(3));
                insurance.CompanyName = GetValueFromField(fields.ElementAtOrDefault(4));
                var street = GetValueFromField(fields.ElementAtOrDefault(5));
                var city = GetValueFromField(fields.ElementAtOrDefault(5), componentIndex: 2);
                var postalCode = GetValueFromField(fields.ElementAtOrDefault(5), componentIndex: 4);
                var countryCode = GetValueFromField(fields.ElementAtOrDefault(5), componentIndex: 5);
                if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(postalCode) || !string.IsNullOrEmpty(countryCode))
                    insurance.CompanyAddress = new Address() { Street = street, City = city, PostalCode = postalCode, CountryCode = countryCode };
                insurance.CompanyPhone = GetValueFromField(fields.ElementAtOrDefault(6));
                insurance.StartDate = GetValueFromField(fields.ElementAtOrDefault(12)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmmss", "dd/MM/yyyy");
                insurance.EndDate = GetValueFromField(fields.ElementAtOrDefault(13)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmmss", "dd/MM/yyyy");
                insurance.PlanType = GetValueFromField(fields.ElementAtOrDefault(15));
                insurance.InsuredLastName = GetValueFromField(fields.ElementAtOrDefault(16));
                insurance.InsuredFirstName = GetValueFromField(fields.ElementAtOrDefault(16), componentIndex: 1);
                insurance.InsuredRelationToPatient = GetValueFromField(fields.ElementAtOrDefault(17));
                street = GetValueFromField(fields.ElementAtOrDefault(19));
                city = GetValueFromField(fields.ElementAtOrDefault(19), componentIndex: 2);
                postalCode = GetValueFromField(fields.ElementAtOrDefault(19), componentIndex: 4);
                countryCode = GetValueFromField(fields.ElementAtOrDefault(19), componentIndex: 5);
                if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(postalCode) || !string.IsNullOrEmpty(countryCode))
                    insurance.InsuredAddress = new Address() { Street = street, City = city, PostalCode = postalCode, CountryCode = countryCode };
                street = GetValueFromField(fields.ElementAtOrDefault(44));
                city = GetValueFromField(fields.ElementAtOrDefault(44), componentIndex: 2);
                postalCode = GetValueFromField(fields.ElementAtOrDefault(44), componentIndex: 4);
                countryCode = GetValueFromField(fields.ElementAtOrDefault(44), componentIndex: 5);
                if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(postalCode) || !string.IsNullOrEmpty(countryCode))
                    insurance.InsuredEmployerAddress = new Address() { Street = street, City = city, PostalCode = postalCode, CountryCode = countryCode };
                if ((new string[] { "A01", "A02", "A04", "A05", "A08", "A14", "A15" }).Contains(eventType))
                {
                    var kg1kg2 = GetValueFromField(fields.ElementAtOrDefault(47));
                    if (!string.IsNullOrEmpty(kg1kg2))
                    {
                        var kg1kg2Parts = kg1kg2.Split('/');
                        insurance.KG1 = kg1kg2Parts.FirstOrDefault();
                        insurance.KG2 = kg1kg2Parts.Length > 1 ? kg1kg2Parts[1] : null;
                    }
                }
                insurance.InsuredId = GetValueFromField(fields.ElementAtOrDefault(49));

                if (patient.Insurances == null)
                    patient.Insurances = new List<Insurance>();
                patient.Insurances.Add(insurance);
            }
            else if (messageType == "MFN")
            {
                insuranceCompany.PlanId = GetValueFromField(fields.ElementAtOrDefault(2));
                insuranceCompany.Id = GetValueFromField(fields.ElementAtOrDefault(3));
                insuranceCompany.Name = GetValueFromField(fields.ElementAtOrDefault(4));
                var street = GetValueFromField(fields.ElementAtOrDefault(5));
                var city = GetValueFromField(fields.ElementAtOrDefault(5), componentIndex: 2);
                var postalCode = GetValueFromField(fields.ElementAtOrDefault(5), componentIndex: 4);
                var countryCode = GetValueFromField(fields.ElementAtOrDefault(5), componentIndex: 5);
                if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(postalCode) || !string.IsNullOrEmpty(countryCode))
                    insuranceCompany.Address = new Address() { Street = street, City = city, PostalCode = postalCode, CountryCode = countryCode };
                insuranceCompany.Phone = GetValueFromField(fields.ElementAtOrDefault(6));
            }
        }

        async Task ProcessOBX(string segment)
        {
            var fields = segment.Split(fieldSep);

            var observation = new Observation();

            var index = GetValueFromField(fields.ElementAtOrDefault(1));
            observation.Type = GetValueFromField(fields.ElementAtOrDefault(2));
            observation.Id = GetValueFromField(fields.ElementAtOrDefault(3));
            observation.SubId = GetValueFromField(fields.ElementAtOrDefault(4));
            observation.Value = GetValueFromField(fields.ElementAtOrDefault(5));
            observation.Description = GetValueFromField(fields.ElementAtOrDefault(5), componentIndex: 1);
            observation.ResultStatus = GetValueFromField(fields.ElementAtOrDefault(11));
            observation.InActive = observation.ResultStatus == "D" ? (bool?)true : null;
            observation.Date = GetValueFromField(fields.ElementAtOrDefault(14)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmmss", "dd/MM/yyyy");

            switch (eventType)
            {
                case "A28":
                case "A31":
                    if (index == "1")
                        patient.Observations = null;
                    if (patient.Observations == null)
                        patient.Observations = new List<Observation>();
                    if (observation.Id == "EID_IMAGE")
                    {
                        var eTag = await httpClient.HeadAsync(string.Format(photoUri, patient.Id), fromCache: true).ConfigureAwait(false);
                        if (observation.InActive.GetValueOrDefault())
                            await httpClient.DeleteAsync(string.Format(photoUri, patient.Id)).ConfigureAwait(false);
                        else
                        {
                            var base64 = GetValueFromField(fields.ElementAtOrDefault(5), componentIndex: 4);
                            var eid = Convert.FromBase64String(base64);
                            await httpClient.PutAsync(string.Format(photoEidUri, patient.Id), eid, eTag: eTag).ConfigureAwait(false);
                        }
                    }
                    else
                        patient.Observations.Add(observation);
                    break;
                case "A01":
                case "A02":
                case "A04":
                case "A05":
                case "A08":
                case "A14":
                    if (visit != null)
                    {
                        if (index == "1")
                            visit.Observations = null;
                        if (visit.Observations == null)
                            visit.Observations = new List<Observation>();
                        visit.Observations.Add(observation);
                    }
                    break;
                case "A03":
                    if (visit != null)
                    {
                        var i = visit.Observations.FindIndex(x => x.Id == observation.Id);
                        if (i == -1)
                        {
                            if (visit.Observations == null)
                                visit.Observations = new List<Observation>();
                            visit.Observations.Add(observation);
                        }
                        else
                            visit.Observations[i] = observation;
                    }
                    break;
                default:
                    break;
            }
        }

        async Task ProcessPV1(string segment)
        {
            var fields = segment.Split(fieldSep);

            var visitNumber = GetValueFromField(fields.ElementAtOrDefault(19));
            if (string.IsNullOrEmpty(visitNumber))
                return;

            visit = await httpClient.GetAsync<Visit>(string.Format(visitUri, visitNumber)).ConfigureAwait(false);
            if (visit == null)
            {
                if ((new string[] { "A01", "A04", "A05", "A14" }).Contains(eventType))
                    visit = new Visit() { Id = visitNumber };
                else
                {
                    if (log.IsWarnEnabled)
                        log.WarnFormat("No visit in DB with number {0} for HL7 with eventType {1} ", visitNumber, eventType);
                    return;
                }
            }

            visit.PatientId = patient.Id;

            transfer = new Transfer();

            visit.PatientClass = GetValueFromField(fields.ElementAtOrDefault(2));

            transfer.WardId = GetValueFromField(fields.ElementAtOrDefault(3));
            transfer.RoomId = GetValueFromField(fields.ElementAtOrDefault(3), componentIndex: 1);
            transfer.BedId = GetValueFromField(fields.ElementAtOrDefault(3), componentIndex: 2);
            transfer.CampusId = GetValueFromField(fields.ElementAtOrDefault(3), componentIndex: 3);
            transfer.LocationStatus = GetValueFromField(fields.ElementAtOrDefault(3), componentIndex: 4);

            visit.AdmissionType = GetValueFromField(fields.ElementAtOrDefault(4));
            if ((new string[] { "A04", "A05" }).Contains(eventType))
                visit.PreadmitNumber = GetValueFromField(fields.ElementAtOrDefault(5));

            transfer.PriorWardId = GetValueFromField(fields.ElementAtOrDefault(6));
            transfer.PriorRoomId = GetValueFromField(fields.ElementAtOrDefault(6), componentIndex: 1);
            transfer.PriorBedId = GetValueFromField(fields.ElementAtOrDefault(6), componentIndex: 2);
            transfer.PriorCampusId = GetValueFromField(fields.ElementAtOrDefault(6), componentIndex: 3);
            transfer.PriorLocationStatus = GetValueFromField(fields.ElementAtOrDefault(6), componentIndex: 4);

            visit.HomeDoctor = GetValueFromField(fields.ElementAtOrDefault(7));
            visit.ReferringDoctor = GetValueFromField(fields.ElementAtOrDefault(8));

            transfer.ConsultingDoctor = GetValueFromField(fields.ElementAtOrDefault(9));
            consultingDoctorLastName = GetValueFromField(fields.ElementAtOrDefault(9), componentIndex: 1);
            consultingDoctorFirstName = GetValueFromField(fields.ElementAtOrDefault(9), componentIndex: 2);

            transfer.DepartmentId = GetValueFromField(fields.ElementAtOrDefault(10));
            transfer.TemporaryLocation = GetValueFromField(fields.ElementAtOrDefault(11));

            visit.AdmittingDoctor = GetValueFromField(fields.ElementAtOrDefault(17));

            transfer.FinancialClass = GetValueFromField(fields.ElementAtOrDefault(20));
            transfer.FinancialClassFromDate = GetValueFromField(fields.ElementAtOrDefault(20), componentIndex: 1).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmm", "dd/MM/yyyy");

            visit.ChargePriceIndicator = GetValueFromField(fields.ElementAtOrDefault(21));
            visit.HomeDoctorRecievesLetter = GetValueFromField(fields.ElementAtOrDefault(22)).Maybe(x => x == "N" ? (bool?)true : null);
            if ((new string[] { "A01", "A02", "A03", "A04", "A08" }).Contains(eventType))
                visit.Internet = GetValueFromField(fields.ElementAtOrDefault(28)).Maybe(x => x == "1" ? (bool?)true : null);
            if ((new string[] { "A03", "A16" }).Contains(eventType))
            {
                visit.DischargeDisposition = GetValueFromField(fields.ElementAtOrDefault(36));
                visit.DischargeToLocation = GetValueFromField(fields.ElementAtOrDefault(37));
            }

            transfer.DepartmentType = GetValueFromField(fields.ElementAtOrDefault(39));

            visit.AdmissionDate = GetValueFromField(fields.ElementAtOrDefault(44)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmm", "dd/MM/yyyy") ?? eventOccured;

            transfer.Pending = (new string[] { "A15", "A16" }).Contains(eventType) ? (bool?)true : null;
            transfer.FromDate = (new string[] { "A01", "A04", "A05", "A14" }).Contains(eventType) ? visit.AdmissionDate : eventOccured;
            visit.Pending = (new string[] { "A05", "A14" }).Contains(eventType) ? (bool?)true : null;

            if ((new string[] { "A03", "A16", "A08" }).Contains(eventType))
                visit.DischargeDate = GetValueFromField(fields.ElementAtOrDefault(45)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmm", "dd/MM/yyyy");
            visit.ExternalId = GetValueFromField(fields.ElementAtOrDefault(50));
        }

        void ProcessPV2(string segment)
        {
            if (visit == null)
                return;

            var fields = segment.Split(fieldSep);

            transfer.RoomAsked = GetValueFromField(fields.ElementAtOrDefault(2));
            transfer.RoomAssigned = GetValueFromField(fields.ElementAtOrDefault(2), componentIndex: 3);

            if ((new string[] { "A01", "A02", "A04", "A05", "A15" }).Contains(eventType))
                visit.ExpectedAdmissionDate = GetValueFromField(fields.ElementAtOrDefault(8)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmm", "dd/MM/yyyy");
            if ((new string[] { "A01", "A02", "A04", "A05", "A16" }).Contains(eventType))
                visit.ExpectedDischargeDate = GetValueFromField(fields.ElementAtOrDefault(9)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmm", "dd/MM/yyyy");
            if ((new string[] { "A01", "A02", "A04", "A05", "A14", "A15" }).Contains(eventType))
                visit.ExpectedAdmissionDays = GetValueFromField(fields.ElementAtOrDefault(10)).Maybe(x => x.ToNullableInt());
            visit.ReferringDoctor2 = GetValueFromField(fields.ElementAtOrDefault(13));
            visit.VisitPublicityCode = GetValueFromField(fields.ElementAtOrDefault(21));
            if ((new string[] { "A01", "A04", "A05" }).Contains(eventType))
                visit.HomeDoctorRecievesLetter = GetValueFromField(fields.ElementAtOrDefault(22)).Maybe(x => x == "N" ? (bool?)true : null);
            visit.ChargeAdjustmentCode = GetValueFromField(fields.ElementAtOrDefault(30));
            visit.NewbornBaby = GetValueFromField(fields.ElementAtOrDefault(36)).Maybe(x => x == "Y" ? (bool?)true : null);
            visit.BabyDetained = GetValueFromField(fields.ElementAtOrDefault(37)).Maybe(x => x == "Y" ? (bool?)true : null);
            visit.MKG = GetValueFromField(fields.ElementAtOrDefault(38));
        }

        void ProcessAL1(string segment)
        {
            var fields = segment.Split(fieldSep);

            var allergy = new Allergy();

            var index = GetValueFromField(fields.ElementAtOrDefault(1));
            if (index == "1")
                patient.Allergies = null;

            allergy.Type = GetValueFromField(fields.ElementAtOrDefault(2));
            allergy.Description = GetValueFromField(fields.ElementAtOrDefault(3));

            if (patient.Allergies == null)
                patient.Allergies = new List<Allergy>();
            patient.Allergies.Add(allergy);
        }

        async Task ProcessMRG(string segment)
        {
            var fields = segment.Split(fieldSep);

            var priorPatientId = GetValueFromField(fields.ElementAtOrDefault(1));
            var priorPatient = new Patient()
            {
                Id = priorPatientId,
                MergedWithId = patient.Id
            };
            await httpClient.PutAsync<Patient>(string.Format(patientUri, priorPatientId), priorPatient).ConfigureAwait(false);
            //await httpClient.DeleteAsync(string.Format(patientUri, priorPatientId)).ConfigureAwait(false);

            //update all visits
            var result = await httpClient.GetAsync<Adt.CouchDb.Result<Visit>>(string.Format(visitsByPatientIdUri, priorPatientId)).ConfigureAwait(false);
            if (result != null)
            {
                var visits = (from row in result.rows
                              select row.doc);
                foreach (var visit in visits)
                {
                    visit.PatientId = patient.Id;
                    await StoreVisit(visit).ConfigureAwait(false);
                }
            }

            //do not store the patient
            patient = null;
        }

        void ProcessMFI(string segment)
        {
            var fields = segment.Split(fieldSep);

            masterFileId = GetValueFromField(fields.ElementAtOrDefault(1));
        }

        async Task ProcessMFE(string segment)
        {
            var fields = segment.Split(fieldSep);

            var id = GetValueFromField(fields.ElementAtOrDefault(1));
            switch (id)
            {
                case "MUP":
                case "MAD":
                    break;
                case "MDL":
                    masterFileDelete = true;
                    break;
            }

            var key = GetValueFromField(fields.ElementAtOrDefault(4));
            switch (masterFileId)
            {
                case "PRA":
                    doctor = await httpClient.GetAsync<Doctor>(string.Format(doctorUri, key)).ConfigureAwait(false);
                    if (doctor == null)
                        doctor = new Doctor() { Id = key };
                    break;
                case "INS":
                    insuranceCompany = await httpClient.GetAsync<InsuranceCompany>(string.Format(insuranceCompanyUri, key)).ConfigureAwait(false);
                    if (insuranceCompany == null)
                        insuranceCompany = new InsuranceCompany() { Id = key };
                    break;
                default:
                    break;
            }
        }

        void ProcessSTF(string segment)
        {
            var fields = segment.Split(fieldSep);

            doctor.Id = GetValueFromField(fields.ElementAtOrDefault(1));
            doctor.LastName = GetValueFromField(fields.ElementAtOrDefault(3));
            doctor.FirstName = GetValueFromField(fields.ElementAtOrDefault(3), componentIndex: 1);
            doctor.HospitalRelation = GetValueFromField(fields.ElementAtOrDefault(4));
            doctor.Sex = GetValueFromField(fields.ElementAtOrDefault(5));
            doctor.BirthDate = GetValueFromField(fields.ElementAtOrDefault(6)).ToNullableDatetime("yyyyMMdd", "yyyyMMddHHmmss", "dd/MM/yyyy");
            doctor.Active = GetValueFromField(fields.ElementAtOrDefault(7)).Maybe(x => x == "A" ? (bool?)true : null);
            doctor.Phones = null;
            doctor.MobilePhones = null;
            doctor.Faxes = null;
            doctor.Emails = null;
            foreach (var repetition in fields.ElementAtOrDefault(10).Split(repetitionSep))
            {
                switch (GetValueFromField(repetition, componentIndex: 2))
                {
                    case "PH":
                        if (doctor.Phones == null)
                            doctor.Phones = new List<string>();
                        doctor.Phones.Add(GetValueFromField(repetition));
                        break;
                    case "CP":
                        if (doctor.MobilePhones == null)
                            doctor.MobilePhones = new List<string>();
                        doctor.MobilePhones.Add(GetValueFromField(repetition));
                        break;
                    case "FX":
                        if (doctor.Faxes == null)
                            doctor.Faxes = new List<string>();
                        doctor.Faxes.Add(GetValueFromField(repetition));
                        break;
                }

                var email = GetValueFromField(repetition, componentIndex: 3);
                if (!string.IsNullOrEmpty(email))
                {
                    if (doctor.Emails == null)
                        doctor.Emails = new List<string>();
                    doctor.Emails.Add(email);
                }
            }
            var street = GetValueFromField(fields.ElementAtOrDefault(11));
            var city = GetValueFromField(fields.ElementAtOrDefault(11), componentIndex: 2);
            var postalCode = GetValueFromField(fields.ElementAtOrDefault(11), componentIndex: 4);
            var countryCode = GetValueFromField(fields.ElementAtOrDefault(11), componentIndex: 5);
            if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(postalCode) || !string.IsNullOrEmpty(countryCode))
                doctor.Address = new Address() { Street = street, City = city, PostalCode = postalCode, CountryCode = countryCode };
            doctor.PreferredContactMethod = GetValueFromField(fields.ElementAtOrDefault(16));
            doctor.PreferredContactSubMethod = GetValueFromField(fields.ElementAtOrDefault(16), componentIndex: 1);
            doctor.Title = GetValueFromField(fields.ElementAtOrDefault(18));
        }

        void ProcessPRA(string segment)
        {
            var fields = segment.Split(fieldSep);

            doctor.Type = GetValueFromField(fields.ElementAtOrDefault(3));
            doctor.RizivNr = GetValueFromField(fields.ElementAtOrDefault(6));
        }

        #endregion
    }
}