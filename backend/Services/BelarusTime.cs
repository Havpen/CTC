namespace CurrencyRates.Api.Services;

public static class BelarusTime
{
    private static readonly TimeZoneInfo Zone = FindZone();

    public static DateOnly Today()
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);
        return DateOnly.FromDateTime(local);
    }

    private static TimeZoneInfo FindZone()
    {
        foreach (var id in new[] { "Europe/Minsk", "Belarus Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
                return zone;
        }

        return TimeZoneInfo.Local;
    }
}
