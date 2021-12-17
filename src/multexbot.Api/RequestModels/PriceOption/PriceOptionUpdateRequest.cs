using System.ComponentModel.DataAnnotations;

namespace multexBot.Api.RequestModels.PriceOption
{
    public class PriceOptionUpdateRequest
    {
        [Required]
        public long Id { get; set; }
        
        [Required]
        public long BotId { get; set; }
        
        public long? RunTime { get; set; }
        
        public long? EndTime { get; set; }
        
        [Required]
        public bool IsActive { get; set; }
    }
}