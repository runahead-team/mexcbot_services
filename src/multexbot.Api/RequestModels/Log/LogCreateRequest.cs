namespace multexBot.Api.RequestModels.Log
{
    public class LogCreateRequest
    {
        public long BotId { get; set; }
        
        public string Message { get; set; }
        
        public string ErrorCode { get; set; }
        
        public long Time { get; set; }
    }
}