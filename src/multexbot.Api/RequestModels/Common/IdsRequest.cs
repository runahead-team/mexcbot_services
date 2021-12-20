using System.ComponentModel.DataAnnotations;

namespace multexbot.Api.RequestModels.Common
{
    public class IdsRequest
    {
        [Required]
        public long[] Ids { get; set; }
    }
}