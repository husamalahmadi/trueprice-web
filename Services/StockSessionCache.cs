namespace TruePrice.Services
{
    public class StockSessionCache
    {
        public Dictionary<string, decimal>? CachedPrices { get; set; }
        public DateTime CachedAt { get; set; }
    }

}