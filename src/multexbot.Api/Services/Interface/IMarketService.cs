using System.Collections.Generic;
using System.Threading.Tasks;
using multexbot.Api.Models.Market;
using multexbot.Api.Models.PriceOption;
using multexbot.Api.RequestModels.Market;
using multexbot.Api.RequestModels.PriceOption;
using multexbot.Api.ResponseModels.Market;
using multexbot.Api.ResponseModels.PriceOption;
using sp.Core.Models;

namespace multexbot.Api.Services.Interface
{
    public interface IMarketService
    {
        Task<MarketDto> SysGet(string symbol);

        Task SysUpdatePrice();
    }
}