using System;

namespace Adt
{
    [Serializable]
    public class Observation
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public string SubId { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string ResultStatus { get; set; }
        public bool? InActive { get; set; }
        public DateTime? Date { get; set; }
    }
}
