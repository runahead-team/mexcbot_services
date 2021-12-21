using System.ComponentModel.DataAnnotations;

namespace multexbot.Api.RequestModels.Market
{
    public class AdmMarketUpdateRequest
    {
        [Required]
        public long Id { get; set; }
        
        public bool IsActive { get; set; }
    }
}