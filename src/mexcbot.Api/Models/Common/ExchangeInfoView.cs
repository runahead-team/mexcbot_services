using Io.Gate.GateApi.Model;
using mexcbot.Api.Models.LBank;
using mexcbot.Api.Models.DeepCoin;
using mexcbot.Api.Models.CoinStore;
using mexcbot.Api.Models.Bybit;
using sp.Core.Extensions;

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
            QuoteAmountPrecision = lBankExchangeInfo.MinTranQua;
            QuoteAssetPrecision = int.TryParse(lBankExchangeInfo.PriceAccuracy, out var priceAccuracyValue)
                ? priceAccuracyValue
                : 0;
            BaseAssetPrecision = int.TryParse(lBankExchangeInfo.QuantityAccuracy, out var qtyAccuracyValue)
                ? qtyAccuracyValue
                : 0;
        }

        public ExchangeInfoView(DeepCoinExchangeInfo deepCoinExchangeInfo)
        {
            Symbol = deepCoinExchangeInfo.Symbol;
            MinQty = deepCoinExchangeInfo.MinSz;

            QuoteAssetPrecision = deepCoinExchangeInfo.TickSz.CountPrecision();
            BaseAssetPrecision = deepCoinExchangeInfo.LotSz.CountPrecision();
            MaxLimitQty = deepCoinExchangeInfo.MaxLimitSz;
            Status = deepCoinExchangeInfo.State;
        }

        public ExchangeInfoView(CoinStoreExchangeInfo deepCoinExchangeInfo)
        {
            Symbol = deepCoinExchangeInfo.Symbol;
            MinQty = deepCoinExchangeInfo.MinLimitSize;

            var quoteAssetPrecision = 0;
            if (int.TryParse(deepCoinExchangeInfo.TickSz, out var parsedTickSz) && parsedTickSz > 0)
                quoteAssetPrecision = parsedTickSz;

            var baseAssetPrecision = 0;
            if (int.TryParse(deepCoinExchangeInfo.LotSz, out var parsedLotSz) && parsedLotSz > 0)
                baseAssetPrecision = parsedLotSz;

            QuoteAssetPrecision = quoteAssetPrecision;
            BaseAssetPrecision = baseAssetPrecision;
            Status = deepCoinExchangeInfo.State;
        }

        public ExchangeInfoView(CurrencyPair gateInfo)
        {
            Symbol = gateInfo.Id;
            MinQty = gateInfo.MinBaseAmount;
            MaxLimitQty = gateInfo.MaxBaseAmount;

            BaseAssetPrecision = gateInfo.AmountPrecision;
            QuoteAssetPrecision = gateInfo.Precision;

            Status = gateInfo.TradeStatus.GetValueOrDefault().ToString();
        }

        public ExchangeInfoView(BybitExchangeInfo bybitInfo)
        {
            Symbol = bybitInfo.Symbol;
            MinQty = bybitInfo.LotSizeFilter?.MinOrderQty;
            MaxLimitQty = bybitInfo.LotSizeFilter?.MaxOrderQty;

            BaseAssetPrecision = (bybitInfo.LotSizeFilter?.BasePrecision ?? "0").CountPrecision();
            QuoteAssetPrecision = (bybitInfo.LotSizeFilter?.QuotePrecision ?? "0").CountPrecision();

            Status = bybitInfo.Status;
        }

        public string Symbol { get; set; }

        public int QuotePrecision { get; set; }

        public int QuoteAssetPrecision { get; set; }

        public int BaseAssetPrecision { get; set; }

        public decimal BaseSizePrecision { get; set; }

        public string QuoteAmountPrecision { get; set; }

        public string MinQty { get; set; }
        public string MaxLimitQty { get; set; }

        public string[] OrderTypes { get; set; }

        public string[] Permissions { get; set; }

        public string Status { get; set; }
    }
}