using COZO.KMEHR;

namespace COZO
{
    public interface IPatientConsentTransaction
    {
        transaction GetEmptyPatientConsentTransaction();
        transaction GetPatientConsentTransaction();
        transaction GetRevokePatientConsentTransaction();
    }
}
