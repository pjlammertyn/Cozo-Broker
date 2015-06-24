using System;

namespace Adt
{
    [Serializable]
    public class InsuranceCompany
    {   
        public string PlanId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
        public string Phone { get; set; }
    }
}
