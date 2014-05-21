using COZO.KMEHR;

namespace COZO
{
    public interface IResultsClient
    {
        string GetDeclareTransactionPutMessage(string patientId, params transaction[] transactions);
    }
}
