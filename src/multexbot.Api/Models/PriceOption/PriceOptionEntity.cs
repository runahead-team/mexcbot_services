using multexbot.Api.RequestModels.PriceOption;
using sp.Core.Utils;

namespace multexbot.Api.Models.PriceOption
{
    public class PriceOptionEntity
    {
        public PriceOptionEntity(){}

        public PriceOptionEntity(PriceOptionCreateRequest request)
        {
            Guid = AppUtils.NewGuidStr();
            BotId = request.BotId;
            Price = request.Price;
            IsActive = request.IsActive;
        }
        
        public PriceOptionEntity(PriceOptionUpdateRequest request)
        {
            BotId = request.BotId;
            RunTime = request.RunTime;
            EndTime = request.EndTime;
            IsActive = request.IsActive;
        }
        
        public long Id { get; set; }
        
        public string Guid { get; set; }

        public long BotId { get; set; }
        
        public decimal Price { get; set; }
        
        public long? RunTime { get; set; }
        
        public long? EndTime { get; set; }
        
        public bool IsActive { get; set; }
    }
}