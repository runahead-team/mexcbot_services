using System.Threading.Tasks;
using mexcbot.Api.Models.Bot;
using mexcbot.Api.ResponseModels.Order;
using sp.Core.Models;

namespace mexcbot.Api.Services.Interface
{
    public interface IBotService
    {
        Task<PagingResult<BotDto>> GetBotsAsync(TableRequest request, AppUser appUser);

        Task<BotDto> CreateAsync(BotDto request, AppUser appUser);
        
        Task UpdateAsync(BotDto request, AppUser appUser);

        Task<PagingResult<OrderDto>> GetOrderHistoryAsync(TableRequest request, AppUser appUser);
    }
}