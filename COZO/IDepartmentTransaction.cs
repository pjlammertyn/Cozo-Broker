using COZO.KMEHR;

namespace COZO
{
    public interface IDepartmentTransaction
    {
        transaction GetDepartmentTransaction(string departmentId);
        //CDHCPARTYvalues GetDepartment(string departmentId);
        hcpartyType GetDepartmentHcpartyType(string aHCParty, string departmentId);
    }
}
