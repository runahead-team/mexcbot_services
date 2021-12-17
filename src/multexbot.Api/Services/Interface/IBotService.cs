using System.Collections.Generic;
using System.Threading.Tasks;
using multexBot.Api.Constants;
using multexBot.Api.Models.ApiKey;
using multexBot.Api.Models.Bot;
using sp.Core.Models;

namespace multexBot.Api.Services.Interface
{
    public interface IBotService
    {
        Task<List<BotView>> GetList(ExchangeType exchange, AppUser user);

        Task<BotView> Create(BotUpsertRequest request, AppUser user);

        Task Update(BotUpsertRequest request, AppUser user);

        Task Delete(long id, AppUser user);

        Task Run();

        Task CancelExpiredOrder();

        Task ClearOrderJob();
    }
}