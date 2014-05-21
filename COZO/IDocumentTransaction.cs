using COZO.KMEHR;
using System;
using System.Collections.Generic;

namespace COZO
{
    public interface IDocumentTransaction
    {
        transaction GetDocumentTransactionDetail(string key);
        IList<transaction> GetDocumentTransactionList(string patientId, DateTime fromDate, DateTime? until, int? offset, int maxResults, decimal? visitNumber);
        IEnumerable<PatientDeclareTransaction> GetDocumentDeclareTransactionList(DateTime fromDate, DateTime toDate);
        IEnumerable<PatientDeclareTransaction>/*IDictionary<string, IEnumerable<transaction>>*/ GetDocumentDeclareTransactionListGroupedByPatientId(DateTime fromDate);
    }

    public class PatientDeclareTransaction
    {
        public string PatientId { get; set; }
        public dynamic Item { get; set; }
        public Func<dynamic, transaction> TransactionFunc { get; set; }
    }
}
