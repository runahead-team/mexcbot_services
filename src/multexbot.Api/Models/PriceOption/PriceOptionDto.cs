namespace multexBot.Api.Models.PriceOption
{
    public class PriceOptionDto
    {
        public long Id { get; set; }
        
        public string Guid { get; set; }

        public long BotId { get; set; }
        
        public decimal Price { get; set; }
        
        public long? RunTime { get; set; }
        
        public long? EndTime { get; set; }
        
        public bool IsActive { get; set; }
    }
}