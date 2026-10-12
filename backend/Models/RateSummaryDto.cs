namespace CurrencyRates.Api.Models;

public sealed class RateSummaryDto
{
    public decimal Average { get; init; }
    public decimal Min { get; init; }
    public decimal Max { get; init; }
}
