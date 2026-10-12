using CurrencyRates.Api.Models;
using CurrencyRates.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyRates.Api.Controllers;

[ApiController]
[Route("api/currencies")]
public sealed class CurrenciesController : ControllerBase
{
    private readonly RateService _rates;

    public CurrenciesController(RateService rates)
    {
        _rates = rates;
    }

    [HttpGet]
    public Task<IReadOnlyList<CurrencyDto>> Get(CancellationToken cancellationToken)
        => _rates.GetCurrenciesAsync(cancellationToken);
}
