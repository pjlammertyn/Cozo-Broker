using COZO.KMEHR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace COZO
{
    public interface IVisitTransaction
    {
        Task<transaction> GetAdmissionTransaction(string visitId);
        Task<transaction> GetDischargeTransaction(string visitId);
        Task<transaction> GetDeleteAdmissionTransactionOnlyEncounterNumber(string visitId);
        Task<IList<transaction>> GetTransferTransactions(string visitId);
    }
}
