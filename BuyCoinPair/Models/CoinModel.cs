using Newtonsoft.Json;

namespace BuyCoinPair.Models
{
    public class CoinModel
    {
        [JsonProperty("Pair")]
        public List<PairModel>? Pair { get; set; }
        [JsonProperty("Order")]
        public long Order { get; set; }
    }

    public class PairModel
    {
        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("Order")]
        public int Order { get; set; }
    }

    public class BinanceDataReceivedModel
    {
        [JsonProperty("symbol")]
        public string? Symbol { get; set; } = string.Empty;
        [JsonProperty("price")]
        public decimal Price { get; set; }
    }
}
