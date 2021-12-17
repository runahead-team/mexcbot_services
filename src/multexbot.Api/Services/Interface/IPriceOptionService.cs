using System.Collections.Generic;
using System.Threading.Tasks;
using multexBot.Api.Models.PriceOption;
using multexBot.Api.RequestModels.PriceOption;
using multexBot.Api.ResponseModels.PriceOption;

namespace multexBot.Api.Services.Interface
{
    public interface IPriceOptionService
    {
        Task<List<PriceOptionView>> GetList(long botId);

        Task<PriceOptionView> Create(PriceOptionCreateRequest request);

        Task Update(PriceOptionUpdateRequest request);

        Task<List<PriceOptionDto>> SysGetList(long botId);
    }
}