using CurrencyRates.Api.Models;

namespace CurrencyRates.Api.Services;

public interface IRateProvider
{
    Task<IReadOnlyList<CurrencyDto>> GetCurrenciesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RateQuote>> GetMonthRatesAsync(string currencyCode, int year, int month, CancellationToken cancellationToken);

    Task<RateQuote?> GetRateOnDateAsync(string currencyCode, DateOnly date, CancellationToken cancellationToken);
}
