using CurrencyRates.Api.Models;
using CurrencyRates.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyRates.Api.Controllers;

[ApiController]
[Route("api/rates")]
public sealed class RatesController : ControllerBase
{
    private readonly RateService _rates;

    public RatesController(RateService rates)
    {
        _rates = rates;
    }

    [HttpGet]
    public Task<IReadOnlyList<DailyRateDto>> Get(string currency, int year, int month, CancellationToken cancellationToken)
        => _rates.GetMonthRatesAsync(currency, year, month, cancellationToken);

    [HttpGet("summary")]
    public Task<RateSummaryDto> Summary(string currency, int year, int month, CancellationToken cancellationToken)
        => _rates.GetSummaryAsync(currency, year, month, cancellationToken);
}
