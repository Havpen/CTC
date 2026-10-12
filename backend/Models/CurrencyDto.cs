namespace CurrencyRates.Api.Models;

public sealed class CurrencyDto
{
    public int Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public int Scale { get; init; }
}
