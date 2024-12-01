using Newtonsoft.Json;
using mexcbot.Api.Constants;

namespace mexcbot.Api.ResponseModels.Order
{
    public class DeepCoinCanceledOrderView
    {
        [JsonProperty("instId")]
        public string Symbol { get; set; }
        
        [JsonProperty("clOrdId")]
        public string OrigClientOrderId { get; set; }
        
        [JsonProperty("ordId")]
        public string OrderId { get; set; }

        public string ClientOrderId => OrigClientOrderId;
    }
}