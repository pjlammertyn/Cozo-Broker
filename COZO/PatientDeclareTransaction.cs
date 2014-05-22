using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COZO
{
    public class PatientDeclareTransaction
    {
        public string PatientId { get; set; }
        public dynamic Item { get; set; }
        public Func<dynamic, transaction> TransactionFunc { get; set; }
    }
}
