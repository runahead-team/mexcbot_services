using System.Threading.Tasks;
using mexcbot.Api.Models.Bot;
using mexcbot.Api.RequestModels.Bot;
using mexcbot.Api.ResponseModels.Order;
using sp.Core.Models;

namespace mexcbot.Api.Services.Interface
{
    public interface IBotService
    {
        Task<PagingResult<BotDto>> GetBotsAsync(TableRequest request, AppUser appUser);

        Task<BotDto> GetBot(BotGetRequest request, AppUser appUser);

        Task<BotDto> CreateAsync(BotUpsertRequest request, AppUser appUser);
        
        Task UpdateAsync(BotUpsertRequest request, AppUser appUser);

        Task UpdateStatusAsync(BotUpdateStatusRequest request, AppUser appUser);

        Task<PagingResult<OrderDto>> GetOrderHistoryAsync(TableRequest request, AppUser appUser);

        Task DeleteBotAsync(long botId, AppUser appUser);

        Task UpdateBotHistory(BotHistoryDto data);
    }
}