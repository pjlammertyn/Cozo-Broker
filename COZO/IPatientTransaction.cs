using COZO.KMEHR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace COZO
{
    public interface IPatientTransaction
    {
        Task<bool> HasValidSocialSecurityNumber(string patientId);
        personType GetPersonTypeOnlyHospitalId(string patientId);
        personType GetPersonTypeOnlySocialSecurityNumber(string patientId);
        //transaction GetPhysicianPatientInformationTransaction(string patientId);
        Task<transaction> GetGeneralPractitionerTherapeuticLinkTransaction(string patientId);
        Task<personType> GetPersonType(string patientId, string socialSecurityNumber = null);
        Task<transaction> GetExtendedPatientInformationTransaction(string patientId);
        Task<bool> AllowGeneralPractitionerAccessToCozo(string patientId);
        Task<bool> ValidPatientId(string patientId);
        Task<string> GetPatientIdByRizivNr(string socialSecurityNumber);
        Task<IList<string>> GetAllVisitIds(string patientId);
    }
}
