namespace UKHO.ExchangeSets.Fulfilment.IIC
{
    public interface IIicClientFactory
    {
        Task<IIicClient> CreateIicClientAsync();
    }
}
