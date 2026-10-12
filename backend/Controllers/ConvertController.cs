using CurrencyRates.Api.Models;
using CurrencyRates.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyRates.Api.Controllers;

[ApiController]
[Route("api/convert")]
public sealed class ConvertController : ControllerBase
{
    private readonly RateService _rates;

    public ConvertController(RateService rates)
    {
        _rates = rates;
    }

    [HttpGet]
    public Task<ConvertResultDto> Get(string from, string to, decimal amount, CancellationToken cancellationToken)
        => _rates.ConvertAsync(from, to, amount, cancellationToken);
}
