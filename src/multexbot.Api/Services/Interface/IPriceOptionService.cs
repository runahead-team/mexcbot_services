using System.Collections.Generic;
using System.Threading.Tasks;
using multexbot.Api.Models.PriceOption;
using multexbot.Api.RequestModels.PriceOption;
using multexbot.Api.ResponseModels.PriceOption;

namespace multexbot.Api.Services.Interface
{
    public interface IPriceOptionService
    {
        Task<List<PriceOptionView>> GetList(long botId);

        Task<PriceOptionView> Create(PriceOptionCreateRequest request);

        Task Update(PriceOptionUpdateRequest request);

        Task<List<PriceOptionDto>> SysGetList(long botId);
    }
}