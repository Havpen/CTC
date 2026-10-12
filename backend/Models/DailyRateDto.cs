namespace CurrencyRates.Api.Models;

public sealed class DailyRateDto
{
    public DateOnly Date { get; init; }
    public decimal OfficialRate { get; init; }
    public decimal? Change { get; init; }
}
