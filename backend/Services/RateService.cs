using System.Globalization;
using CurrencyRates.Api.Models;

namespace CurrencyRates.Api.Services;

public sealed class RateService
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private readonly IRateProvider _rates;

    public RateService(IRateProvider rates)
    {
        _rates = rates;
    }

    public async Task<IReadOnlyList<CurrencyDto>> GetCurrenciesAsync(CancellationToken cancellationToken)
    {
        var currencies = await _rates.GetCurrenciesAsync(cancellationToken);

        return currencies
            .Where(currency => !currency.Code.Equals("BYN", StringComparison.OrdinalIgnoreCase))
            .Append(Byn())
            .OrderBy(currency => currency.Code, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<IReadOnlyList<DailyRateDto>> GetMonthRatesAsync(string currencyCode, int year, int month, CancellationToken cancellationToken)
    {
        var code = Normalize(currencyCode);
        ValidatePeriod(year, month);

        if (code == "BYN")
            return BuildBynMonth(year, month);

        var published = await _rates.GetMonthRatesAsync(code, year, month, cancellationToken);
        if (published.Count == 0)
            throw new RateException(StatusCodes.Status404NotFound, "Нет курсов за выбранный период");

        var ordered = published.OrderBy(quote => quote.Date).ToList();
        var firstDay = ordered[0].Date;
        var lastDay = ordered[^1].Date;
        var byDate = ordered
            .GroupBy(quote => quote.Date)
            .ToDictionary(group => group.Key, group => group.Last().OfficialRate);

        var dayBefore = firstDay.AddDays(-1);
        var previousQuote = await _rates.GetRateOnDateAsync(code, dayBefore, cancellationToken);
        decimal? prior = previousQuote is not null && previousQuote.Date <= dayBefore
            ? previousQuote.OfficialRate
            : null;

        var rows = new List<DailyRateDto>();
        decimal? carry = null;

        for (var day = firstDay; day <= lastDay; day = day.AddDays(1))
        {
            decimal rate;
            if (byDate.TryGetValue(day, out var publishedRate))
                rate = publishedRate;
            else if (carry is decimal known)
                rate = known;
            else
                continue;

            carry = rate;
            rows.Add(new DailyRateDto
            {
                Date = day,
                OfficialRate = rate,
                Change = prior is decimal before ? rate - before : null
            });
            prior = rate;
        }

        if (rows.Count == 0)
            throw new RateException(StatusCodes.Status404NotFound, "Нет курсов за выбранный период");

        return rows;
    }

    public async Task<RateSummaryDto> GetSummaryAsync(string currencyCode, int year, int month, CancellationToken cancellationToken)
    {
        var rows = await GetMonthRatesAsync(currencyCode, year, month, cancellationToken);

        return new RateSummaryDto
        {
            Average = Math.Round(rows.Average(row => row.OfficialRate), 4, MidpointRounding.AwayFromZero),
            Min = rows.Min(row => row.OfficialRate),
            Max = rows.Max(row => row.OfficialRate)
        };
    }

    public async Task<ConvertResultDto> ConvertAsync(string from, string to, decimal amount, CancellationToken cancellationToken)
    {
        var source = Normalize(from);
        var target = Normalize(to);
        if (amount <= 0)
            throw new RateException(StatusCodes.Status400BadRequest, "Сумма должна быть больше нуля");

        var today = BelarusTime.Today();
        if (source == target)
        {
            return new ConvertResultDto
            {
                Amount = amount,
                From = source,
                To = target,
                Result = amount,
                RateDate = today,
                Description = "Валюты совпадают, сумма не изменилась"
            };
        }

        var sourceQuote = await GetQuoteAsync(source, today, cancellationToken);
        var targetQuote = await GetQuoteAsync(target, today, cancellationToken);
        DateOnly rateDate;

        if (source == "BYN")
            rateDate = targetQuote.Date;
        else if (target == "BYN")
            rateDate = sourceQuote.Date;
        else if (sourceQuote.Date != targetQuote.Date)
        {
            rateDate = sourceQuote.Date < targetQuote.Date ? sourceQuote.Date : targetQuote.Date;
            sourceQuote = await GetQuoteAsync(source, rateDate, cancellationToken);
            targetQuote = await GetQuoteAsync(target, rateDate, cancellationToken);
            rateDate = sourceQuote.Date < targetQuote.Date ? sourceQuote.Date : targetQuote.Date;
        }
        else
            rateDate = sourceQuote.Date;

        var result = Math.Round(amount * PerUnit(sourceQuote) / PerUnit(targetQuote), 4, MidpointRounding.AwayFromZero);

        return new ConvertResultDto
        {
            Amount = amount,
            From = source,
            To = target,
            Result = result,
            RateDate = rateDate,
            Description = Describe(source, target, sourceQuote, targetQuote, rateDate)
        };
    }

    private async Task<RateQuote> GetQuoteAsync(string code, DateOnly date, CancellationToken cancellationToken)
    {
        if (code == "BYN")
        {
            return new RateQuote
            {
                Date = date,
                Code = "BYN",
                Scale = 1,
                OfficialRate = 1m
            };
        }

        var quote = await _rates.GetRateOnDateAsync(code, date, cancellationToken);
        if (quote is null)
            throw new RateException(StatusCodes.Status404NotFound, $"Нет курса {code} на {date.ToString("dd.MM.yyyy", Ru)}");

        return quote;
    }

    private static IReadOnlyList<DailyRateDto> BuildBynMonth(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var today = BelarusTime.Today();
        if (end > today)
            end = today;

        var rows = new List<DailyRateDto>();
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            rows.Add(new DailyRateDto
            {
                Date = day,
                OfficialRate = 1m,
                Change = 0m
            });
        }

        return rows;
    }

    private static void ValidatePeriod(int year, int month)
    {
        if (month is < 1 or > 12 || year is < 1995 or > 2100)
            throw new RateException(StatusCodes.Status400BadRequest, "Некорректный месяц или год");

        var start = new DateOnly(year, month, 1);
        if (start > BelarusTime.Today())
            throw new RateException(StatusCodes.Status404NotFound, "Нет курсов за выбранный период");
    }

    private static string Normalize(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new RateException(StatusCodes.Status400BadRequest, "Укажите валюту");

        return currencyCode.Trim().ToUpperInvariant();
    }

    private static decimal PerUnit(RateQuote quote)
    {
        if (quote.Scale <= 0 || quote.OfficialRate <= 0)
            throw new RateException(StatusCodes.Status502BadGateway, "Некорректный курс НБРБ");

        return quote.OfficialRate / quote.Scale;
    }

    private static string Describe(string source, string target, RateQuote sourceQuote, RateQuote targetQuote, DateOnly rateDate)
    {
        var date = rateDate.ToString("dd.MM.yyyy", Ru);
        if (source == "BYN")
            return $"Расчёт выполнен по курсу НБРБ на {date}: {FormatQuote(targetQuote)}";

        if (target == "BYN")
            return $"Расчёт выполнен по курсу НБРБ на {date}: {FormatQuote(sourceQuote)}";

        return $"Расчёт выполнен по курсу НБРБ на {date}: {FormatQuote(sourceQuote)}; {FormatQuote(targetQuote)}";
    }

    private static string FormatQuote(RateQuote quote)
    {
        return $"{quote.Scale} {quote.Code} = {quote.OfficialRate.ToString("F4", Ru)} BYN";
    }

    private static CurrencyDto Byn()
    {
        return new CurrencyDto
        {
            Id = 0,
            Code = "BYN",
            Name = "Белорусский рубль",
            Scale = 1
        };
    }
}
