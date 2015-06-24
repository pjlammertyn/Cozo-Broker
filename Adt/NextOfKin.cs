using System;
using System.Collections.Generic;
using Adt.Json;
using Newtonsoft.Json;

namespace Adt
{
    [Serializable]
    public class NextOfKin
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Address Address { get; set; }
        public IList<string> Phones { get; set; }
        public IList<string> MobilePhones { get; set; }
        public IList<string> Faxes { get; set; }
        public IList<string> Emails { get; set; }
        public string Type { get; set; }
        public string Index { get; set; }
        [JsonConverter(typeof(IsoDateConverter))]
        public DateTime? FromDate { get; set; }
        public string Language { get; set; }
    }
}
