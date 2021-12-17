using multexBot.Api.Models.PriceOption;

namespace multexBot.Api.ResponseModels.PriceOption
{
    public class PriceOptionView
    {
        public PriceOptionView(){}

        public PriceOptionView(PriceOptionEntity priceOption)
        {
            Id = priceOption.Id;
            Price = priceOption.Price;
            IsActive = priceOption.IsActive;
        }
        
        public long Id { get; set; }
        
        public decimal Price { get; set; }
        
        public bool IsActive { get; set; }
    }
}