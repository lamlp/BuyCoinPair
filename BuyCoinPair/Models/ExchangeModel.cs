using Newtonsoft.Json;

namespace BuyCoinPair.Models
{
    public class ExchangeRecivedModel
    {
        [JsonProperty("symbols")]
        public List<ExchangeModel> Symbols { get; set; }
    }

    public class ExchangeModel
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }
        [JsonProperty("filters")]
        public List<FilterModel> Filters { get; set; }
    }

    public class FilterModel
    {
        [JsonProperty("filterType")]
        public string FilterType { get; set; }
        [JsonProperty("minQty")]
        public decimal MinQty { get; set; }
        [JsonProperty("maxQty")]
        public decimal MaxQty { get; set; }
        [JsonProperty("stepSize")]
        public decimal StepSize { get; set; }
    }
}
