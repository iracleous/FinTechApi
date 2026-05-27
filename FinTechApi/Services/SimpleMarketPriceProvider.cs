namespace FinTechApi.Services
{
    public class SimpleMarketPriceProvider : IMarketPriceProvider
    {
        public Task<decimal> GetPriceAsync(string instrumentSymbol, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
