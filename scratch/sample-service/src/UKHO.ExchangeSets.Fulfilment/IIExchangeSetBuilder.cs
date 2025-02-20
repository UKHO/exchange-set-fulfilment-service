namespace UKHO.ExchangeSets.Fulfilment
{
    public interface IIExchangeSetBuilder
    {
        Task<ExchangeSetBuilderResult> BuildExchangeSet();

    }
}
