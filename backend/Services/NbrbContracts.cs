using System.Text.Json.Serialization;

namespace CurrencyRates.Api.Services;

internal sealed class NbrbCurrency
{
    [JsonPropertyName("Cur_ID")]
    public int CurId { get; set; }

    [JsonPropertyName("Cur_Abbreviation")]
    public string CurAbbreviation { get; set; } = "";

    [JsonPropertyName("Cur_Name")]
    public string CurName { get; set; } = "";

    [JsonPropertyName("Cur_Scale")]
    public int CurScale { get; set; }

    [JsonPropertyName("Cur_Periodicity")]
    public int CurPeriodicity { get; set; }

    [JsonPropertyName("Cur_DateStart")]
    public DateTime CurDateStart { get; set; }

    [JsonPropertyName("Cur_DateEnd")]
    public DateTime CurDateEnd { get; set; }
}

internal sealed class NbrbRate
{
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("Cur_Abbreviation")]
    public string? CurAbbreviation { get; set; }

    [JsonPropertyName("Cur_Scale")]
    public int CurScale { get; set; }

    [JsonPropertyName("Cur_OfficialRate")]
    public decimal CurOfficialRate { get; set; }
}

internal sealed class NbrbDynamicRate
{
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("Cur_OfficialRate")]
    public decimal CurOfficialRate { get; set; }
}
