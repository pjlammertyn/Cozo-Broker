using COZO.KMEHR;

namespace COZO
{
    public interface IWardTransaction
    {
        transaction GetWardTransaction(string wardId);
    }
}
