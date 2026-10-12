namespace CurrencyRates.Api.Services;

public sealed class RateException : Exception
{
    public int StatusCode { get; }

    public RateException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
