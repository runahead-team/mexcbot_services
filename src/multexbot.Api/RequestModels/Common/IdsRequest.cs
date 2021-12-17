using System.ComponentModel.DataAnnotations;

namespace multexBot.Api.RequestModels.Common
{
    public class IdsRequest
    {
        [Required]
        public long[] Ids { get; set; }
    }
}