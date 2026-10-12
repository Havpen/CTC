namespace CurrencyRates.Api.Models;

public sealed class ConvertResultDto
{
    public decimal Amount { get; init; }
    public string From { get; init; } = "";
    public string To { get; init; } = "";
    public decimal Result { get; init; }
    public DateOnly RateDate { get; init; }
    public string Description { get; init; } = "";
}
