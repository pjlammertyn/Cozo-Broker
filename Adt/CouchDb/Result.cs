using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adt.CouchDb
{
    [Serializable]
    public class Result<T>
    {
        public IEnumerable<Row<T>> rows { get; set; }
    }
}
