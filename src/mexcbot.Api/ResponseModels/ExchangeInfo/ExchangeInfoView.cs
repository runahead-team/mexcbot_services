namespace mexcbot.Api.ResponseModels.ExchangeInfo
{
    public class ExchangeInfoView
    {
        public string Symbol { get; set; }
        
        public int QuotePrecision { get; set; }
        
        public int QuoteAssetPrecision { get; set; }
        
        public decimal BaseSizePrecision { get; set; }
        
        public string QuoteAmountPrecision { get; set; }
    }
}