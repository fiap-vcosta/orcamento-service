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
        Environment.SetEnvironmentVariable("PubSub__ProjectId", "integration");
        Environment.SetEnvironmentVariable("PubSub__UseInMemory", "true");
        Environment.SetEnvironmentVariable("PubSub__EnableWorkers", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Firestore:ProjectId"] = "test-project",
                ["Firestore:DatabaseId"] = "orcamento",
                ["MercadoPago:AccessToken"] = "test-token",
                ["MercadoPago:WebhookSecret"] = WebhookSecret,
                ["PubSub:ProjectId"] = "integration",
                ["PubSub:UseInMemory"] = "true",
                ["PubSub:EnableWorkers"] = "false"
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
