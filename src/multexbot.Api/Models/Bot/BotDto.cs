using System.ComponentModel.DataAnnotations;
using multexBot.Api.Constants;
using multexBot.Api.Models.ApiKey;
using Newtonsoft.Json;
using sp.Core.Constants;
using sp.Core.Exceptions;
using sp.Core.Models;
using sp.Core.Utils;

namespace multexBot.Api.Models.Bot
{
    public class BotDto
    {
        public BotDto()
        {
        }

        public BotDto(BotUpsertRequest request, AppUser user)
        {
            if (request.Id == 0)
            {
                Guid = AppUtils.NewGuidStr();
            }
            else
            {
                Id = request.Id;
                Guid = request.Guid;
            }

            UserId = user.Id;
            Email = user.Email;

            Name = request.Name;
            ExchangeType = request.ExchangeType;
            ApiKey = request.ApiKey;
            SecretKey = request.SecretKey;
            Symbol = request.Base + request.Quote;
            Base = request.Base;
            Quote = request.Quote;
            Side = request.Side;
            IsActive = request.IsActive;
            LastExecute = 0;
            NextTime = 0;
            LastPrice = 0;
            RootId = request.RootId;

            Options = JsonConvert.SerializeObject(request.Options);

            #region Validate

            if (string.IsNullOrEmpty(Base)
                || string.IsNullOrEmpty(Quote)
                || string.IsNullOrEmpty(Name))
                throw new AppException(AppError.INVALID_PARAMETERS, "Base,Quote,Name");

            var option = request.Options;

            if (option.MinQty < 0
                || option.MaxQty < 0
                || option.MinTradePerExec <= 0
                || option.MaxTradePerExec <= 0
                || option.MinInterval <= 0
                || option.MaxInterval <= 0
                || option.BasePrice < 0
                || option.MinStopPrice < 0
                || option.MaxStopPrice < 0
                || option.StopLossBase < 0
                || option.StopLossQuote < 0
                || option.OrderExp < 0)
                throw new AppException(AppError.INVALID_PARAMETERS, JsonConvert.SerializeObject(option));

            if (option.MinPriceStep > option.MaxPriceStep)
                throw new AppException(AppError.INVALID_PARAMETERS, "MinPriceStep > MaxPriceStep");

            if (option.MinInterval > option.MaxInterval)
                throw new AppException(AppError.INVALID_PARAMETERS, "MinInterval > MaxInterval");

            if (option.MinTradePerExec > option.MaxTradePerExec)
                throw new AppException(AppError.INVALID_PARAMETERS, "MinInterval > MaxTradePerExec");

            #endregion
        }

        public long Id { get; set; }

        public string Guid { get; set; }

        public long UserId { get; set; }

        public string Email { get; set; }

        [MaxLength(32)] public string Name { get; set; }

        public ExchangeType ExchangeType { get; set; }

        [MaxLength(128)] public string ApiKey { get; set; }

        [MaxLength(128)] public string SecretKey { get; set; }

        public string Symbol { get; set; }

        [MaxLength(32)] public string Base { get; set; }

        [MaxLength(32)] public string Quote { get; set; }

        public OrderSide Side { get; set; }

        public bool IsActive { get; set; }

        public long LastExecute { get; set; }

        public long NextTime { get; set; }

        public decimal LastPrice { get; set; }

        public decimal LastPriceUsd { get; set; }

        public string Log { get; set; }

        //Json Serialize of BotOption
        public string Options { get; set; }

        #region Options

        public int OrderExp { get; set; }

        public decimal OpenPrice { get; set; }

        #endregion
        
        public long? RootId { get; set; }
    }
}