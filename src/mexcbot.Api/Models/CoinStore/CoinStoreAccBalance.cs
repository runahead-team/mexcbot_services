using System.Globalization;
using Newtonsoft.Json;

namespace mexcbot.Api.Models.CoinStore
{
    public class CoinStoreAccBalance
    {
        [JsonProperty("currency")]
        public string Asset { get; set; }
        
        //status 1: available 4: deactivated
        [JsonProperty("type")]
        public decimal Type { get; set; }
        
        [JsonProperty("balance")]
        public decimal Value { get; set; }

        public string Free => Type == 1 ? Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        
        public string Frozen => Type == 4 ? Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }
}