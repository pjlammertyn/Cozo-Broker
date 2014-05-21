using COZO.KMEHR;

namespace COZO
{
    public interface IHealthcarePartyTransaction
    {
        hcpartyType GetDoctorHcpartyTypeOnlySSNumberByInternalCode(string doctorCode);
        hcpartyType GetDoctorHcpartyTypeOnlySSNumberLastNameAndName(string rizivNr);
        hcpartyType GetDoctorHcpartyTypeOnlySSNumberLastNameAndNameByInternalCode(string doctorCode);
        transaction GetDoctorTransaction(string rizivNr);
        transaction GetDoctorTransactionByInternalCode(string doctorCode);
        transaction GetAdministratorTransaction();
    }
}
