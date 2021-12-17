using DefaultNamespace;
using multexBot.Api.RequestModels.Log;
using sp.Core.Utils;

namespace multexBot.Api.Models.ApiKey
{
    public class LogEntity
    {
        public LogEntity(){}

        public LogEntity(LogCreateRequest request)
        {
            BotId = request.BotId;
            Message = request.Message;
            ErrorCode = request.ErrorCode;
            Time = AppUtils.NowMilis();
        }
        
        public long Id { get; set; }
        
        public long BotId { get; set; }
        
        public string Message { get; set; }
        
        public string ErrorCode { get; set; }
        
        public long Time { get; set; }
    }
}