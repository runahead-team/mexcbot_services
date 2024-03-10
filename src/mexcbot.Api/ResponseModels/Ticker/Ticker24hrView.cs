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
            
            //todo
            
            // //Cal quote volume
            // var volAsDecimal = decimal.TryParse(lBankTicker24Hr.Vol, out var volValue) ? volValue : 0;
            // if(volAsDecimal > 0)
            //     var quoteVolumeAsDecimal=volAsDecimal* 
        }

        public string Symbol { get; set; }

        public string Volume { get; set; }

        public string QuoteVolume { get; set; }

        public long OpenTime { get; set; }

        public long CloseTime { get; set; }

        public string LastPrice { get; set; }
    }
}