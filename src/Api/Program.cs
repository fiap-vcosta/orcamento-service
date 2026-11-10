using Api.Extensions;
using BuildingBlocks.Http.Logging;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog(DatadogLogging.Configure);
    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
    });

    var app = builder.Build();
    app.UseApiConfiguration();

    app.MapGet("/orcamento/health", () => Results.Ok("OK")).AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação encerrada inesperadamente");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
