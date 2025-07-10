using System.Net.Http.Json;
using System.Text.Json;

namespace TruePrice.Services
{
    public class StockApiService
    {
        private readonly HttpClient _http;
        private const string ApiKey = "5da413057f75498490b0303582e0d0de";
        private const string BaseUrl = "https://api.twelvedata.com";

        public StockApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<string>> GetSymbolsAsync(string exchange)
        {
            var url = $"{BaseUrl}/stocks?exchange={exchange}&apikey={ApiKey}";
            var response = await _http.GetFromJsonAsync<JsonElement>(url);

            return response.GetProperty("data")
                           .EnumerateArray()
                           .Select(stock => stock.GetProperty("symbol").GetString() + ":TADAWUL")
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .ToList();
        }

        public async Task<Dictionary<string, decimal>> GetPricesAsync(List<string> symbols)
        {
            var result = new Dictionary<string, decimal>();

            foreach (var chunk in symbols.Chunk(100))
            {
                var joined = string.Join(",", chunk);
                var url = $"{BaseUrl}/price?symbol={joined}&apikey={ApiKey}";

                JsonElement pricesResponse;

                try
                {
                    pricesResponse = await _http.GetFromJsonAsync<JsonElement>(url);
                }
                catch
                {
                    continue; // Skip this chunk if response is invalid
                }

                if (pricesResponse.ValueKind == JsonValueKind.Object)
                {
                    foreach (var item in pricesResponse.EnumerateObject())
                    {
                        if (item.Value.ValueKind == JsonValueKind.Object &&
                            item.Value.TryGetProperty("price", out var priceProp) &&
                            decimal.TryParse(priceProp.GetString(), out var price))
                        {
                            result[item.Name] = price;
                        }
                    }
                }
                else if (pricesResponse.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in pricesResponse.EnumerateArray())
                    {
                        if (item.TryGetProperty("symbol", out var sym) &&
                            item.TryGetProperty("price", out var priceProp) &&
                            sym.ValueKind == JsonValueKind.String &&
                            decimal.TryParse(priceProp.GetString(), out var price))
                        {
                            result[sym.GetString()!] = price;
                        }
                    }
                }
            }

            return result;
        }
    }
}
