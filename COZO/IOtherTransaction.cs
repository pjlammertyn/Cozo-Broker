using COZO.KMEHR;
using System;
using System.Collections.Generic;

namespace COZO
{
    public interface IOtherTransaction
    {
        transaction GetOtherTransactionDetail(string key);
        IList<transaction> GetOtherTransactionList(string patientId, DateTime fromDate, DateTime? until, int? offset, int maxResults, decimal? visitNumber);
    }
}
