using multexBot.Api.Constants;

namespace multexBot.Api.Models.ApiKey
{
    public class ApiKeyView
    {
        public ApiKeyView(){}
        
        public long Id { get; set; }

        public string Guid { get; set; }
        
        public long UserId { get; set; }

        public string ApiKey { get; set; }

        public string SecretKey { get; set; }
        
        public ExchangeType ExchangeType { get; set; }
    }
}