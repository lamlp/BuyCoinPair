using Newtonsoft.Json;

namespace BuyCoinPair.Models
{
    public class OrderDepthModel
    {
        [JsonProperty("bids")]
        public List<decimal[]> Bids { get; set; }
        [JsonProperty("asks")]
        public List<decimal[]> Asks { get; set; }
    }
}
