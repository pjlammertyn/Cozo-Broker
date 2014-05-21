using COZO.KMEHR;
using System;
using System.Collections.Generic;

namespace COZO
{
    public interface IImageTransaction
    {
        transaction GetImageTransactionDetail(string key);
        IList<transaction> GetImageTransactionList(string patientId, DateTime fromDate, DateTime? until, int? offset, int maxResults, decimal? visitNumber);
        IEnumerable<PatientDeclareTransaction> GetImageDeclareTransactionList(DateTime fromDate, DateTime toDate);
        IEnumerable<PatientDeclareTransaction>/*IDictionary<string, IEnumerable<transaction>>*/ GetImageDeclareTransactionListGroupedByPatientId(DateTime fromDate);
    }
}
