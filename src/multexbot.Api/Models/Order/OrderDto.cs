using multexBot.Api.Constants;

namespace DefaultNamespace
{
    public class OrderDto
    {
        public long Id { get; set; }

        public string Guid { get; set; }
        
        public long BotId { get; set; }

        public long UserId { get; set; }

        public long ExternalId { get; set; }
        
        public string Symbol { get; set; }

        public string Base { get; set; }

        public string Quote { get; set; }

        public OrderSide Side { get; set; }
        
        public decimal Qty { get; set; }

        public decimal Price { get; set; }

        public decimal Total { get; set; }

        public decimal Filled { get; set; }

        public OrderStatus Status { get; set; }

        public long Time { get; set; }
        
        public long ExpiredTime { get; set; }
        
        public bool IsExpired { get; set; }

        #region DTO

        public ExchangeType ExchangeType { get; set; }
        
        public string ApiKey { get; set; }

        public string SecretKey { get; set; }

        #endregion
    }
}