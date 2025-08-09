using Newtonsoft.Json;

namespace mexcbot.Api.Models.Bybit
{
    public class BybitExchangeInfo
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("baseCoin")]
        public string BaseCoin { get; set; }

        [JsonProperty("quoteCoin")]
        public string QuoteCoin { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("lotSizeFilter")]
        public BybitLotSizeFilter LotSizeFilter { get; set; }

        [JsonProperty("priceFilter")]
        public BybitPriceFilter PriceFilter { get; set; }
    }

    public class BybitLotSizeFilter
    {
        [JsonProperty("basePrecision")]
        public string BasePrecision { get; set; }

        [JsonProperty("quotePrecision")]
        public string QuotePrecision { get; set; }

        [JsonProperty("minOrderQty")]
        public string MinOrderQty { get; set; }

        [JsonProperty("maxOrderQty")]
        public string MaxOrderQty { get; set; }

        [JsonProperty("minOrderAmt")]
        public string MinOrderAmt { get; set; }

        [JsonProperty("maxOrderAmt")]
        public string MaxOrderAmt { get; set; }
    }

    public class BybitPriceFilter
    {
        [JsonProperty("tickSize")]
        public string TickSize { get; set; }
    }
}
