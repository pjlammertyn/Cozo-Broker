using COZO.KMEHR;

namespace COZO
{
    public interface IGlobalData
    {
        hcpartyType HOSPITALhcparty { get; }
        hcpartyType GZOhcparty { get; }
        string GZOsystem { get; }
        string Version { get; }
        string Hospital { get; }
        string FullHospitalName { get; }
        string HospitalRizivNr { get; }
        string Seperator { get; }

        string ClientAE { get; }
        int ClientPort { get; }
        string RemoteAE { get; }
        string RemoteHost { get; }
        int RemotePort { get; }
        string DicomStoragePath { get; }
    }
}
