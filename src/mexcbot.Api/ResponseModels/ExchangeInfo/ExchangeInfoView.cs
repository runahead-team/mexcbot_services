namespace mexcbot.Api.ResponseModels.ExchangeInfo
{
    public class ExchangeInfoView
    {
        public string Symbol { get; set; }
        
        public int QuotePrecision { get; set; }
        
        public int QuoteAssetPrecision { get; set; }
        
        public int BaseAssetPrecision { get; set; }
        
        public decimal BaseSizePrecision { get; set; }
        
        public string QuoteAmountPrecision { get; set; }
    }
}