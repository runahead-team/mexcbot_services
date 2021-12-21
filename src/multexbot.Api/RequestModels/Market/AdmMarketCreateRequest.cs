using System.ComponentModel.DataAnnotations;

namespace multexbot.Api.RequestModels.Market
{
    public class AdmMarketCreateRequest
    {
        [Required]
        public string Coin { get; set; }
        
        [Required]
        public bool IsActive { get; set; }
    }
}