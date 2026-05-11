using System.Net.Http;
using System.Text.Json;

namespace ProjectOOP
{
    public class CryptoApiService
    {
        private readonly HttpClient _client = new();
        private const string BaseUrl = "https://api.coingecko.com/api/v3";

        public CryptoApiService()
        {
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            _client.DefaultRequestHeaders.Add("x-cg-demo-api-key", "CG-c8zjqr9jv9izrbkBqhvUMfAe");
            _client.Timeout = TimeSpan.FromSeconds(60);
        }

        // Haal top N coins op met prijs, marktcap, volume en sparkline (7d)
        public async Task<List<CryptoModel>> GetTopCoinsAsync(int count = 50)
        {
            var url = $"{BaseUrl}/coins/markets" +
                      $"?vs_currency=usd" +
                      $"&order=market_cap_desc" +
                      $"&per_page={count}" +
                      $"&page=1" +
                      $"&sparkline=true" +
                      $"&price_change_percentage=24h";

            var json = await _client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            var result = new List<CryptoModel>();
            int rank = 1;

            // loop door elk item in de JSON-array en maak een CryptoModel ervan
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var sparkline = new List<double>();
                if (elem.TryGetProperty("sparkline_in_7d", out var sp) &&
                    sp.TryGetProperty("price", out var prices))
                {
                    foreach (var p in prices.EnumerateArray())
                        sparkline.Add(p.GetDouble());
                }

                result.Add(new CryptoModel
                {
                    Id = elem.GetProperty("id").GetString(),
                    Name = elem.GetProperty("name").GetString(),
                    Symbol = elem.GetProperty("symbol").GetString().ToUpper(),
                    Rank = rank++,
                    // TryGetProperty haalt "current_price" op in en steekt da in "pr", ValueKind checkt of het een getal is
                    // (CoinGecko kan null terugsturen), anders crasht GetDouble(). Fallback = 0.
                    Price = elem.TryGetProperty("current_price", out var pr) && pr.ValueKind == JsonValueKind.Number ? pr.GetDouble() : 0,
                    Change24h = elem.TryGetProperty("price_change_percentage_24h", out var ch) && ch.ValueKind == JsonValueKind.Number ? ch.GetDouble() : 0,
                    MarketCap = elem.TryGetProperty("market_cap", out var mc) && mc.ValueKind == JsonValueKind.Number ? mc.GetDouble() : 0,
                    Volume24h = elem.TryGetProperty("total_volume", out var vol) && vol.ValueKind == JsonValueKind.Number ? vol.GetDouble() : 0,
                    CirculatingSupply = elem.TryGetProperty("circulating_supply", out var cs) && cs.ValueKind == JsonValueKind.Number ? cs.GetDouble() : 0,
                    SparklineData = sparkline
                });
            }
            return result;
        }
    }
}
