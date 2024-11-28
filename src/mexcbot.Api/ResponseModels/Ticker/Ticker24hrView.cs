using System.Globalization;
using mexcbot.Api.Models.LBank;

namespace mexcbot.Api.ResponseModels.Ticker
{
    public class Ticker24hrView
    {
        public Ticker24hrView()
        {
        }

        public Ticker24hrView(string baseSymbol, LBankTicker24hr lBankTicker24Hr)
        {
            Symbol = baseSymbol;
            Volume = lBankTicker24Hr.Vol;
            LastPrice = lBankTicker24Hr.Latest;
            QuoteVolume = lBankTicker24Hr.Turnover;

            // //Cal quote volume
            // var volAsDecimal = decimal.TryParse(lBankTicker24Hr.Vol, out var volValue) ? volValue : 0;
            // var lastPriceAsDecimal =
            //     decimal.TryParse(lBankTicker24Hr.Latest, out var lastPriceValue) ? lastPriceValue : 0;
            //
            // if (volAsDecimal > 0)
            //     QuoteVolume = (volAsDecimal * lastPriceAsDecimal).ToString(CultureInfo.InvariantCulture);
        }
        
        public Ticker24hrView(DeepCoinTicker24hr deepCoinTicker24Hr)
        {
            Symbol = deepCoinTicker24Hr.Symbol;
            Volume = deepCoinTicker24Hr.Vol;
            LastPrice = deepCoinTicker24Hr.Latest;
            QuoteVolume = deepCoinTicker24Hr.Turnover;
        }

        public string Symbol { get; set; }

        public string Volume { get; set; }

        public string QuoteVolume { get; set; }

        public long OpenTime { get; set; }

        public long CloseTime { get; set; }

        public string LastPrice { get; set; }
    }
}