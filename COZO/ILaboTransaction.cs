using COZO.KMEHR;
using System;
using System.Collections.Generic;

namespace COZO
{
    public interface ILaboTransaction
    {
        transaction GetLaboTransactionDetail(string key);
        IList<transaction> GetLaboTransactionList(string patientId, DateTime fromDate, DateTime? until, int? offset, int maxResults, decimal? visitNumber);
        IEnumerable<PatientDeclareTransaction> GetLaboDeclareTransactionList(DateTime fromDate, DateTime toDate);
        IEnumerable<PatientDeclareTransaction>/*IDictionary<string, IEnumerable<transaction>>*/ GetLaboDeclareTransactionListGroupedByPatientId(DateTime fromDate);
    }
}
