using Newtonsoft.Json;

namespace mexcbot.Api.Models.Bybit
{
    public class BybitOrder
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("orderLinkId")]
        public string OrderLinkId { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("orderType")]
        public string OrderType { get; set; }

        [JsonProperty("qty")]
        public string OrigQty { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("timeInForce")]
        public string TimeInForce { get; set; }

        [JsonProperty("orderStatus")]
        public string Status { get; set; }

        [JsonProperty("createTime")]
        public string CreateTime { get; set; }

        [JsonProperty("cumExecQty")]
        public string ExecutedQty { get; set; }

        [JsonProperty("cumExecValue")]
        public string CummulativeQuoteQty { get; set; }

        [JsonProperty("cumExecFee")]
        public string CummulativeFeeQty { get; set; }
    }
}
