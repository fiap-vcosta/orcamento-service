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

    if (builder.Environment.IsDevelopment())
    {
        TryLoadLocalJwtPublicKey(builder);
    }

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

    static void TryLoadLocalJwtPublicKey(WebApplicationBuilder webApplicationBuilder)
    {
        var publicKeyPath = Path.GetFullPath(
            Path.Combine(webApplicationBuilder.Environment.ContentRootPath, "..", "..", ".local", "jwt", "public.pem"));
        if (!File.Exists(publicKeyPath))
        {
            return;
        }

        var publicPem = File.ReadAllText(publicKeyPath);
        webApplicationBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Funcionario:PublicKeyPem"] = publicPem,
            ["Jwt:Cliente:PublicKeyPem"] = publicPem
        });
    }
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
