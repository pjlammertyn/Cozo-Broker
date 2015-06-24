using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adt.ElasticSearch
{
    [Serializable]
    public class Result<T>
    {
        public Hits<T> hits { get; set; }
    }
}
