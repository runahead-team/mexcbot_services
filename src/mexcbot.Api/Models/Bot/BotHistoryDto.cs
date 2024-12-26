namespace mexcbot.Api.Models.Bot;

public class BotHistoryDto
{
    public long Id { get; set; }

    public long Date { get; set; }

    public long BotId { get; set; }

    public string BalanceBase { get; set; }

    public string BalanceQuote { get; set; }

    public decimal Spread { get; set; }
}