namespace CurrencyRates.Api.Models;

public sealed class RateQuote
{
    public DateOnly Date { get; init; }
    public string Code { get; init; } = "";
    public int Scale { get; init; }
    public decimal OfficialRate { get; init; }
}
