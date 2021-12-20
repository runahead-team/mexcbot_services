using System.ComponentModel.DataAnnotations;

namespace multexbot.Api.RequestModels.User
{
    public class GaSetupRequest
    {
        [Required]
        public string Password { get; set; }

        [Required]
        public string GaCode { get; set; }

        [Required]
        public bool Enable { get; set; }
    }
}