using System;
using System.ComponentModel.DataAnnotations;
using sp.Core.Extensions;

namespace multexbot.Api.RequestModels.PriceOption
{
    public class PriceOptionCreateRequest
    {
        [Required]
        public long BotId { get; set; }
        
        [Range(0,Int32.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public bool IsActive { get; set; }
    }
}