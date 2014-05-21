using System;
using System.ServiceModel;

namespace COZO
{
    [ServiceContract(Namespace = "http://tempuri.org/GZOService/GZOService")]
    [XmlSerializerFormat]
    public interface IGZOServer : IDisposable
    {
        [OperationContract(Action = "http://tempuri.org/GZOService/GZOService/ping")]
        string ping();
        [OperationContract(Action = "http://tempuri.org/GZOService/GZOService/getTransaction")]
        string getTransaction(string aKmehrXMLString);
        [OperationContract(Action = "http://tempuri.org/GZOService/GZOService/putTransaction")]
        string putTransaction(string aKmehrXMLString);
        [OperationContract(Action = "http://tempuri.org/GZOService/GZOService/echo")]
        string echo(string aKmehrXMLString);
    }
}
