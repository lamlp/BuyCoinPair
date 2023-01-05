using Newtonsoft.Json;

namespace BuyCoinPair.Models
{
    public class WebsocketDepthModel
    {
        [JsonProperty("s")]
        public string Name { get; set; }
        [JsonProperty("b")]
        public List<decimal[]> Bids { get; set; }
        [JsonProperty("a")]
        public List<decimal[]> Asks { get; set; }
    }

    public class WebsocketCoinModel
    {
        public string Name { get; set; }
        public decimal BidPrice { get; set; }
        public decimal AskPrice { get; set; }
    }

    // "{\"method\": \"SUBSCRIBE\",\"params\" :[\"btcusdt@depth\", \"bnbusdt@depth\"],\"id\": 1}";
    public class WebsocketRequestModel
    {
        [JsonProperty("method")]
        public string method { get; set; }
        [JsonProperty("params")]
        public string[] Params { get; set; }
        [JsonProperty("id")]
        public long id { get; set; }
    }
}
