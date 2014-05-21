using System.Threading.Tasks;

namespace COZO
{
    public interface IADTClient
    {
        string GetAdministratorPutMessage();
        string GetCampusPutMessage(string campusId);
        string GetDepartmentPutMessage(string departmentId);
        string GetDoctorPutMessage(string doctorId);
        Task<string> GetPatientGetMessage(string patientId);
        Task<string> GetPatientPutMessage(string patientId);
        Task<string> GetVisitDeleteMessage(string visitId, string patientId);
        Task<string> GetVisitPutMessage(string visitId, string patientId);
        string GetWardPutMessage(string wardId);
        string GetCurrentadmissionlistGetMessage();
        Task<string> GetPatientconsentGetMessage(string patientId);
        Task<string> GetPatientconsentPutMessage(string patientId);
        Task<string> GetRevokepatientconsentPutMessage(string patientId);
    }
}
