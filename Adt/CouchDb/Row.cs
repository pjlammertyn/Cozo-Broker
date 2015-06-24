using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adt.CouchDb
{
    [Serializable]
    public class Row<T>
    {
        public T doc { get; set; }
    }
}
