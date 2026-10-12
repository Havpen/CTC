using CurrencyRates.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyRates.Api.Services;

public sealed class NbrbRateProvider : IRateProvider
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(1);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public NbrbRateProvider(HttpClient http, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CurrencyDto>> GetCurrenciesAsync(CancellationToken cancellationToken)
    {
        var versions = await GetVersionsAsync(cancellationToken);
        var today = BelarusTime.Today();

        return versions
            .Where(currency => currency.Start <= today && currency.End >= today)
            .GroupBy(currency => currency.Code)
            .Select(group => group.OrderByDescending(currency => currency.Start).First())
            .Select(ToDto)
            .OrderBy(currency => currency.Code, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<RateQuote>> GetMonthRatesAsync(string currencyCode, int year, int month, CancellationToken cancellationToken)
    {
        var code = Normalize(currencyCode);
        var cacheKey = $"nbrb:month:{code}:{year}:{month}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<RateQuote>? cached) && cached is not null)
            return cached;

        var start = new DateOnly(year, month, 1);
        var currency = await FindCurrencyAsync(code, start, cancellationToken);
        var end = start.AddMonths(1).AddDays(-1);
        var url = $"rates/dynamics/{currency.Id}?startDate={start:yyyy-MM-dd}&endDate={end:yyyy-MM-dd}";
        var rows = await GetAsync<List<NbrbDynamicRate>>(url, cancellationToken) ?? [];

        var quotes = rows
            .Select(row => new RateQuote
            {
                Date = DateOnly.FromDateTime(row.Date),
                Code = currency.Code,
                Scale = currency.Scale,
                OfficialRate = row.CurOfficialRate
            })
            .Where(quote => quote.Date.Year == year && quote.Date.Month == month)
            .GroupBy(quote => quote.Date)
            .Select(group => group.Last())
            .OrderBy(quote => quote.Date)
            .ToList();

        _cache.Set(cacheKey, quotes, CacheLifetime);
        return quotes;
    }

    public async Task<RateQuote?> GetRateOnDateAsync(string currencyCode, DateOnly date, CancellationToken cancellationToken)
    {
        var code = Normalize(currencyCode);
        var cacheKey = $"nbrb:rate:{code}:{date:yyyy-MM-dd}";
        if (_cache.TryGetValue(cacheKey, out RateQuote? cached))
            return cached;

        var currency = await FindCurrencyAsync(code, date, cancellationToken);
        var url = $"rates/{currency.Code}?parammode=2&ondate={date:yyyy-MM-dd}";
        var rate = await GetAsync<NbrbRate>(url, cancellationToken);
        if (rate is null || rate.CurOfficialRate <= 0)
            return null;

        var quote = new RateQuote
        {
            Date = DateOnly.FromDateTime(rate.Date),
            Code = currency.Code,
            Scale = rate.CurScale > 0 ? rate.CurScale : currency.Scale,
            OfficialRate = rate.CurOfficialRate
        };

        _cache.Set(cacheKey, quote, CacheLifetime);
        return quote;
    }

    private async Task<IReadOnlyList<CurrencyVersion>> GetVersionsAsync(CancellationToken cancellationToken)
    {
        const string cacheKey = "nbrb:currencies";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<CurrencyVersion>? cached) && cached is not null)
            return cached;

        var rows = await GetAsync<List<NbrbCurrency>>("currencies", cancellationToken);
        if (rows is null)
            throw new RateException(StatusCodes.Status502BadGateway, "Не удалось получить список валют НБРБ");

        var versions = rows
            .Where(row => row.CurPeriodicity == 0 && !string.IsNullOrWhiteSpace(row.CurAbbreviation))
            .Select(row => new CurrencyVersion(
                row.CurId,
                row.CurAbbreviation.Trim().ToUpperInvariant(),
                row.CurName,
                row.CurScale,
                DateOnly.FromDateTime(row.CurDateStart),
                DateOnly.FromDateTime(row.CurDateEnd)))
            .ToList();

        _cache.Set(cacheKey, versions, CacheLifetime);
        return versions;
    }

    private async Task<CurrencyVersion> FindCurrencyAsync(string code, DateOnly date, CancellationToken cancellationToken)
    {
        var currency = (await GetVersionsAsync(cancellationToken))
            .Where(item => item.Code == code && item.Start <= date && item.End >= date)
            .OrderByDescending(item => item.Start)
            .FirstOrDefault();

        if (currency is null)
            throw new RateException(StatusCodes.Status400BadRequest, $"Валюта {code} не найдена");

        return currency;
    }

    private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync(relativeUrl, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return default;

            if (!response.IsSuccessStatusCode)
                throw new RateException(StatusCodes.Status502BadGateway, "Нацбанк не ответил");

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (RateException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            throw new RateException(StatusCodes.Status502BadGateway, "Не удалось получить данные НБРБ");
        }
    }

    private static CurrencyDto ToDto(CurrencyVersion currency)
    {
        return new CurrencyDto
        {
            Id = currency.Id,
            Code = currency.Code,
            Name = currency.Name,
            Scale = currency.Scale
        };
    }

    private static string Normalize(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new RateException(StatusCodes.Status400BadRequest, "Укажите валюту");

        return currencyCode.Trim().ToUpperInvariant();
    }

    private sealed record CurrencyVersion(int Id, string Code, string Name, int Scale, DateOnly Start, DateOnly End);
}
