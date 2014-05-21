using System.ServiceModel;
using System.Threading.Tasks;

namespace COZO
{
    [ServiceContract(Namespace = "http://gzo.be/client")]
    [XmlSerializerFormat]
    public interface IGZOClient
    {
        [OperationContract(Action = "http://gzo.be/client/ping")]
        string ping();
        [OperationContract(Action = "http://gzo.be/client/getTransaction")]
        Task<string> getTransaction(string aKmehrXMLString);
    }
}
