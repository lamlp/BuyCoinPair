using Newtonsoft.Json;

namespace BuyCoinPair.Models
{
    public class OrderModel
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }
        [JsonProperty("orderId")]
        public long OrderId { get; set; }
        [JsonProperty("orderListId")]
        public long OrderListId { get; set; }
        [JsonProperty("clientOrderId")]
        public string ClientOrderId { get; set; }
        [JsonProperty("transactTime")]
        public long TransactTime { get; set; }
        [JsonProperty("price")]
        public decimal Price { get; set; }
        [JsonProperty("origQty")]
        public decimal OrigQty { get; set; }
        [JsonProperty("executedQty")]
        public decimal ExecutedQty { get; set; }
        [JsonProperty("cummulativeQuoteQty")]
        public decimal CummulativeQuoteQty { get; set; }
    }
}
