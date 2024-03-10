using mexcbot.Api.Models.LBank;

namespace mexcbot.Api.ResponseModels.ExchangeInfo
{
    public class ExchangeInfoView
    {
        public ExchangeInfoView()
        {
        }

        public ExchangeInfoView(LBankExchangeInfo lBankExchangeInfo)
        {
            Symbol = lBankExchangeInfo.Symbol;
            QuoteAssetPrecision = int.TryParse(lBankExchangeInfo.PriceAccuracy, out var priceAccuracyValue) ? priceAccuracyValue : 0;
            BaseAssetPrecision = int.TryParse(lBankExchangeInfo.QuantityAccuracy, out var qtyAccuracyValue) ? qtyAccuracyValue : 0;
        }

        public string Symbol { get; set; }

        public int QuotePrecision { get; set; }

        public int QuoteAssetPrecision { get; set; }

        public int BaseAssetPrecision { get; set; }

        public decimal BaseSizePrecision { get; set; }

        public string QuoteAmountPrecision { get; set; }

        public string[] OrderTypes { get; set; }

        public string[] Permissions { get; set; }

        public string Status { get; set; }
    }
}