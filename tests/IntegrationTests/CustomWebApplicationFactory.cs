using Application.Abstractions.Services;
using Google.Cloud.Firestore;
using Infrastructure.Firestore;
using IntegrationTests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string WebhookSecret = "integration-webhook-secret";

    public InMemoryDocumentRecordStore Store { get; } = new();

    public FakeMercadoPagoOrdersClient MercadoPago { get; } = new();

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Firestore__ProjectId", "test-project");
        Environment.SetEnvironmentVariable("Firestore__DatabaseId", "orcamento");
        Environment.SetEnvironmentVariable("MercadoPago__AccessToken", "test-token");
        Environment.SetEnvironmentVariable("MercadoPago__WebhookSecret", WebhookSecret);
        Environment.SetEnvironmentVariable("MercadoPago__ExpirationTime", "P1D");
        Environment.SetEnvironmentVariable("PubSub__ProjectId", "integration");
        Environment.SetEnvironmentVariable("PubSub__UseInMemory", "true");
        Environment.SetEnvironmentVariable("PubSub__EnableWorkers", "false");
        Environment.SetEnvironmentVariable("Jwt__Funcionario__Issuer", TestJwt.Issuer);
        Environment.SetEnvironmentVariable("Jwt__Funcionario__Audience", TestJwt.Audience);
        Environment.SetEnvironmentVariable("Jwt__Funcionario__PublicKeyPem", TestJwt.PublicKeyPem);
        Environment.SetEnvironmentVariable("Jwt__Funcionario__RequireHttpsMetadata", "false");
        Environment.SetEnvironmentVariable("Jwt__Cliente__Issuer", TestJwt.Issuer);
        Environment.SetEnvironmentVariable("Jwt__Cliente__Audience", TestJwt.Audience);
        Environment.SetEnvironmentVariable("Jwt__Cliente__PublicKeyPem", TestJwt.PublicKeyPem);
        Environment.SetEnvironmentVariable("Jwt__Cliente__RequireHttpsMetadata", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var publicPem = TestJwt.PublicKeyPem;
        builder.UseSetting("Jwt:Funcionario:Issuer", TestJwt.Issuer);
        builder.UseSetting("Jwt:Funcionario:Audience", TestJwt.Audience);
        builder.UseSetting("Jwt:Funcionario:PublicKeyPem", publicPem);
        builder.UseSetting("Jwt:Funcionario:RequireHttpsMetadata", "false");
        builder.UseSetting("Jwt:Cliente:Issuer", TestJwt.Issuer);
        builder.UseSetting("Jwt:Cliente:Audience", TestJwt.Audience);
        builder.UseSetting("Jwt:Cliente:PublicKeyPem", publicPem);
        builder.UseSetting("Jwt:Cliente:RequireHttpsMetadata", "false");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Firestore:ProjectId"] = "test-project",
                ["Firestore:DatabaseId"] = "orcamento",
                ["MercadoPago:AccessToken"] = "test-token",
                ["MercadoPago:WebhookSecret"] = WebhookSecret,
                ["MercadoPago:ExpirationTime"] = "P1D",
                ["PubSub:ProjectId"] = "integration",
                ["PubSub:UseInMemory"] = "true",
                ["PubSub:EnableWorkers"] = "false",
                ["Jwt:Funcionario:Issuer"] = TestJwt.Issuer,
                ["Jwt:Funcionario:Audience"] = TestJwt.Audience,
                ["Jwt:Funcionario:PublicKeyPem"] = publicPem,
                ["Jwt:Funcionario:RequireHttpsMetadata"] = "false",
                ["Jwt:Cliente:Issuer"] = TestJwt.Issuer,
                ["Jwt:Cliente:Audience"] = TestJwt.Audience,
                ["Jwt:Cliente:PublicKeyPem"] = publicPem,
                ["Jwt:Cliente:RequireHttpsMetadata"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<FirestoreDb>();
            services.RemoveAll<IDocumentRecordStore>();
            services.RemoveAll<IMercadoPagoOrdersClient>();

            services.AddSingleton<IDocumentRecordStore>(Store);
            services.AddSingleton<IMercadoPagoOrdersClient>(MercadoPago);
        });
    }
}
