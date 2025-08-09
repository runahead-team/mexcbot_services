using System;
using mexcbot.Api.Constants;
using mexcbot.Api.Infrastructure;
using mexcbot.Api.Models.DeepCoin;
using mexcbot.Api.Models.CoinStore;

namespace mexcbot.Api.ResponseModels.Order
{
    public class CanceledOrderView
    {
        public CanceledOrderView()
        {
        }

        public CanceledOrderView(DeepCoinCanceledOrderView deepCoinCanceledOrderView)
        {
            Symbol = deepCoinCanceledOrderView.Symbol;
            OrigClientOrderId = deepCoinCanceledOrderView.OrigClientOrderId;
            OrderId = deepCoinCanceledOrderView.OrderId;
            ClientOrderId = deepCoinCanceledOrderView.ClientOrderId;
        }

        public CanceledOrderView(CoinStoreCanceledOrderView coinStoreCanceledOrderView)
        {
            Symbol = coinStoreCanceledOrderView.Symbol;
            OrigClientOrderId = coinStoreCanceledOrderView.OrigClientOrderId;
            OrderId = coinStoreCanceledOrderView.OrderId.ToString();
            ClientOrderId = coinStoreCanceledOrderView.ClientOrderId;

            var status = OrderStatus.NOT_FOUND;
            if (Enum.TryParse(coinStoreCanceledOrderView.State, out OrderStatus parsedStatus))
                status = parsedStatus;

            Status = status;
        }
        
        public CanceledOrderView(Io.Gate.GateApi.Model.Order gateCanceledOrderView)
        {
            Symbol = gateCanceledOrderView.CurrencyPair;
            OrigClientOrderId = string.Empty;
            OrderId = gateCanceledOrderView.Id;

            Status = BotUtils.GetStatus(gateCanceledOrderView.Status);
        }

        public string Symbol { get; set; }

        public string OrigClientOrderId { get; set; }

        public string OrderId { get; set; }

        public string ClientOrderId { get; set; }

        public string Price { get; set; }

        public string OrigQty { get; set; }

        public string ExecutedQty { get; set; }

        public string CummulativeQuoteQty { get; set; }

        public OrderStatus Status { get; set; }

        public string TimeInForce { get; set; }

        public string Type { get; set; }

        public string Side { get; set; }

        public string LbankOrderStatus { get; set; }
    }
}