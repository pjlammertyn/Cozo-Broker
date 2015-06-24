using System;

namespace OruLabo.CouchDb
{
    [Serializable]
    public class Row<T>
    {
        public T doc { get; set; }
    }
}
