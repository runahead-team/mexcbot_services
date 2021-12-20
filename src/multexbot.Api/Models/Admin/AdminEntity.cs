namespace multexbot.Api.Models.Admin
{
    public class AdminEntity
    {
        public long Id { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string GaSecret { get; set; }

        public bool GaEnable { get; set; }

        public string Role { get; set; }

        public long CreatedTime { get; set; }

    }
}