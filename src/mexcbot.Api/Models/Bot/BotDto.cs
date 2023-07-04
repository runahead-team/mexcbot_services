using System;
using mexcbot.Api.Constants;
using mexcbot.Api.RequestModels.Bot;
using Newtonsoft.Json;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Models;
using sp.Core.Utils;

namespace mexcbot.Api.Models.Bot
{
    public class BotDto
    {
        public BotDto()
        {
        }

        public BotDto(BotUpsertRequest request, AppUser user)
        {
            if (request.Id != 0)
                Id = request.Id;

            UserId = user.Id;
            Base = request.Base;
            Quote = request.Quote;
            VolumeOption = JsonConvert.SerializeObject(request.VolumeOption);
            MakerOption = JsonConvert.SerializeObject(request.MakerOption);
            ApiKey = request.ApiKey;
            ApiSecret = request.ApiSecret;
            Logs = request.Logs;
            Status = request.Status;

            CreatedTime = AppUtils.NowMilis();

            #region Validation

            if (string.IsNullOrEmpty(Base)
                || string.IsNullOrEmpty(Quote))
                throw new AppException(AppError.INVALID_PARAMETERS, "Base,Quote");

            switch (request.Type)
            {
                case BotType.VOLUME:
                {
                    var volumeOption = request.VolumeOption;

                    if (volumeOption.Volume24hr < 0
                        || volumeOption.MinOrderQty < 0
                        || volumeOption.MaxOrderQty <= 0)
                        throw new AppException(AppError.INVALID_PARAMETERS, JsonConvert.SerializeObject(volumeOption));

                    if (volumeOption.MinOrderQty > volumeOption.MaxOrderQty)
                        throw new AppException(AppError.INVALID_PARAMETERS, "MinOrderQty > MaxOrderQty");
                    break;
                }
                case BotType.MAKER:
                {
                    var makerOption = request.MakerOption;

                    if (makerOption.MinQty < 0
                        || makerOption.MaxQty < 0
                        || makerOption.MinTradePerExec <= 0
                        || makerOption.MaxTradePerExec <= 0
                        || makerOption.MinInterval <= 0
                        || makerOption.MaxInterval <= 0
                        || makerOption.BasePrice < 0
                        || makerOption.MinStopPrice < 0
                        || makerOption.MaxStopPrice < 0
                        || makerOption.StopLossBase < 0
                        || makerOption.StopLossQuote < 0
                        || makerOption.OrderExp < 0)
                        throw new AppException(AppError.INVALID_PARAMETERS, JsonConvert.SerializeObject(makerOption));

                    if (makerOption.MinPriceStep > makerOption.MaxPriceStep)
                        throw new AppException(AppError.INVALID_PARAMETERS, "MinPriceStep > MaxPriceStep");

                    if (makerOption.MinInterval > makerOption.MaxInterval)
                        throw new AppException(AppError.INVALID_PARAMETERS, "MinInterval > MaxInterval");

                    if (makerOption.MinTradePerExec > makerOption.MaxTradePerExec)
                        throw new AppException(AppError.INVALID_PARAMETERS, "MinInterval > MaxTradePerExec");
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            #endregion
        }

        public long Id { get; set; }

        public long UserId { get; set; }

        public string Base { get; set; }

        public string Quote { get; set; }

        public string Symbol => $"{Base}{Quote}";

        public BotType Type { get; set; }

        public string VolumeOption { get; set; }
        
        public string MakerOption { get; set; }

        public string ApiKey { get; set; }

        public string ApiSecret { get; set; }

        public string Logs { get; set; }

        public BotStatus Status { get; set; }

        public long LastRunTime { get; set; }
        
        public long NextRunMakerTime { get; set; }

        public long CreatedTime { get; set; }
    }
}