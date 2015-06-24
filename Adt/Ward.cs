using System;
using System.Collections.Generic;

namespace Adt
{
    [Serializable]
    public class Ward
    {
        public string Id { get; set; }
        public string CampusId { get; set; }
        public string Description { get; set; }
        public IList<Bed> Beds { get; set; }
        public IList<Reservation> Reservations { get; set; }
    }
}
