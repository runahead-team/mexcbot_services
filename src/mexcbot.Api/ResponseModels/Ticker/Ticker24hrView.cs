namespace mexcbot.Api.ResponseModels.Ticker
{
    public class Ticker24hrView
    {
        public string Symbol { get; set; }
        
        public string Volume { get; set; }
        
        public string QuoteVolume { get; set; }
        
        public long OpenTime { get; set; }
        
        public long CloseTime { get; set; }
        
        public string LastPrice { get; set; }
    }
}