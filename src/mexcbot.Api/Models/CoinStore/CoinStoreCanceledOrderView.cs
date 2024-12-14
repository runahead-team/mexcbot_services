using Newtonsoft.Json;
using mexcbot.Api.Constants;

namespace mexcbot.Api.ResponseModels.Order
{
    public class CoinStoreCanceledOrderView
    {
        public string Symbol { get; set; }
        
        [JsonProperty("clientOrderId")]
        public string ClientOrderId { get; set; }

        public string OrigClientOrderId => ClientOrderId;
        
        [JsonProperty("ordId")]
        public long OrderId { get; set; }
        
        [JsonProperty("state")]
        public string State { get; set; }
    }
}