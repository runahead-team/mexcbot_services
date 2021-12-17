namespace multexBot.Api.Models.Fund
{
    public class AddressEntity
    {
        public long UserId { get; set; }

        public string Network { get; set; }

        public string Address { get; set; }

        public string AddressTag { get; set; }

        public bool IsDefault { get; set; }

        public long CreatedTime { get; set; }
        
    }
}