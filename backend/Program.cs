using CurrencyRates.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(new { message = "Проверьте параметры запроса" });
    });
builder.Services.AddHttpClient<IRateProvider, NbrbRateProvider>(client =>
{
    var baseUrl = builder.Configuration["Nbrb:BaseUrl"] ?? "https://api.nbrb.by/exrates/";
    if (!baseUrl.EndsWith('/'))
        baseUrl += "/";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("CurrencyRates/1.0");
});
builder.Services.AddScoped<RateService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is OperationCanceledException)
            return;

        var status = exception is RateException rate
            ? rate.StatusCode
            : StatusCodes.Status500InternalServerError;
        var message = exception is RateException
            ? exception.Message
            : "Внутренняя ошибка сервера";

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message });
    });
});

app.MapControllers();
app.Run();
