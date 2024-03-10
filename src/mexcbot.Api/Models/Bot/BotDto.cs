using System;
using System.Collections.Generic;
using mexcbot.Api.Constants;
using mexcbot.Api.Models.Mexc;
using mexcbot.Api.RequestModels.Bot;
using mexcbot.Api.ResponseModels.ExchangeInfo;
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
            Type = request.Type;
            ExchangeType = request.ExchangeType;
            VolumeOption = request.VolumeOption == null
                ? string.Empty
                : JsonConvert.SerializeObject(request.VolumeOption);
            MakerOption = request.MakerOption == null ? string.Empty : JsonConvert.SerializeObject(request.MakerOption);
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

                    if (volumeOption == null)
                        throw new AppException(AppError.INVALID_PARAMETERS, "Volume Option can not be null");

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

                    if (makerOption == null)
                        throw new AppException(AppError.INVALID_PARAMETERS, "Maker Option can not be null");

                    if (makerOption.MinQty < 0
                        || makerOption.MaxQty < 0
                        || makerOption.MinTradePerExec <= 0
                        || makerOption.MaxTradePerExec <= 0
                        || makerOption.MinInterval <= 0
                        || makerOption.MaxInterval <= 0
                        || makerOption.MinStopPrice < 0
                        || makerOption.MaxStopPrice < 0
                        || makerOption.StopLossBase < 0
                        || makerOption.StopLossQuote < 0
                        || makerOption.OrderExp < 0
                        || makerOption.FollowBtcRate < 0)
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

        public BotType Type { get; set; }

        public BotExchangeType ExchangeType { get; set; }

        public string Symbol => ExchangeType == BotExchangeType.LBANK
            ? $"{Base.ToLower()}_{Quote.ToLower()}"
            : $"{Base}{Quote}";

        [JsonIgnore] public string VolumeOption { get; set; }

        public BotVolumeOption VolumeOptionObj => !string.IsNullOrEmpty(VolumeOption)
            ? JsonConvert.DeserializeObject<BotVolumeOption>(VolumeOption)
            : new BotVolumeOption();

        [JsonIgnore] public string MakerOption { get; set; }

        public BotMakerOption MakerOptionObj => !string.IsNullOrEmpty(MakerOption)
            ? JsonConvert.DeserializeObject<BotMakerOption>(MakerOption)
            : new BotMakerOption();

        [JsonIgnore] public string AccountInfo { get; set; }

        public AccInfo AccountInfoObj => !string.IsNullOrEmpty(AccountInfo)
            ? JsonConvert.DeserializeObject<AccInfo>(AccountInfo)
            : new AccInfo();

        [JsonIgnore] public string ExchangeInfo { get; set; }

        public ExchangeInfoView ExchangeInfoObj => !string.IsNullOrEmpty(ExchangeInfo)
            ? JsonConvert.DeserializeObject<ExchangeInfoView>(ExchangeInfo)
            : new ExchangeInfoView();

        public string ApiKey { get; set; }

        public string ApiSecret { get; set; }

        public string Logs { get; set; }

        public BotStatus Status { get; set; }

        public long LastRunTime { get; set; }

        public long NextRunMakerTime { get; set; }

        public long NextRunVolTime { get; set; }

        public long CreatedTime { get; set; }
    }
}