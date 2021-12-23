using System.Collections.Generic;
using System.Threading.Tasks;
using multexbot.Api.Constants;
using multexbot.Api.Models.Bot;
using sp.Core.Models;

namespace multexbot.Api.Services.Interface
{
    public interface IBotService
    {
        Task<List<BotView>> GetList(ExchangeType? exchangeType, AppUser user);

        Task<BotView> Create(BotUpsertRequest request, AppUser user);

        Task Update(BotUpsertRequest request, AppUser user);

        Task Delete(long id, AppUser user);

        Task Run();

        Task CancelExpiredOrder();

        Task ClearOrderJob();
    }
}