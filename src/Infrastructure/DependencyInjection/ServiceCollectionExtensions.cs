using Application.Abstractions.Gateways;
using Application.Abstractions.Services;
using Application.Messaging;
using Application.Services;
using BuildingBlocks.Events;
using BuildingBlocks.Messaging;
using BuildingBlocks.Tracing;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using Infrastructure.Firestore;
using Infrastructure.MercadoPago;
using Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public const string SubscriptionDiagnosticoFinalizado = "orcamento-diagnostico-finalizado";
    public const string SubscriptionOrdemServicoFinalizada = "orcamento-ordem-servico-finalizada";
    public const string SubscriptionOrdemServicoDescartada = "orcamento-ordem-servico-descartada";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FirestoreOptions>(configuration.GetSection(FirestoreOptions.SectionName));
        services.Configure<MercadoPagoOptions>(configuration.GetSection(MercadoPagoOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<FirestoreOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.ProjectId))
            {
                throw new InvalidOperationException("Firestore:ProjectId é obrigatório.");
            }

            var builder = new FirestoreDbBuilder
            {
                ProjectId = options.ProjectId,
                DatabaseId = options.DatabaseId,
                EmulatorDetection = EmulatorDetection.EmulatorOrProduction
            };
            return builder.Build();
        });

        services.AddScoped<IDocumentRecordStore, FirestoreDocumentRecordStore>();
        services.AddScoped<IOrcamentoGateway, FirestoreOrcamentoGateway>();
        services.AddScoped<IOrdemOrcamentoGateway, FirestoreOrdemOrcamentoGateway>();
        services.AddScoped<ICobrancaGateway, FirestoreCobrancaGateway>();
        services.AddScoped<IEventoProcessadoGateway, FirestoreEventoProcessadoGateway>();

        services.AddSingleton<IMercadoPagoWebhookSignatureValidator, MercadoPagoWebhookSignatureValidator>();
        services.AddHttpClient<IMercadoPagoOrdersClient, MercadoPagoOrdersClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<MercadoPagoOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(options.AccessToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);
            }
        });

        services.AddScoped<IOrcamentoIntegrationPublisher, OrcamentoIntegrationPublisher>();
        services.AddScoped<OrcamentoSagaService>();

        var useInMemoryPubSub = configuration.GetValue("PubSub:UseInMemory", false);
        if (useInMemoryPubSub)
        {
            services.AddSingleton<InMemoryPubSubTransport>();
            services.AddSingleton<IPubSubTransport>(sp => sp.GetRequiredService<InMemoryPubSubTransport>());
            services.AddSingleton<IMessageTracePropagator, DatadogMessageTracePropagator>();
            services.AddSingleton<IEventPublisher, EventPublisher>();
            services.AddOptions<PubSubOptions>();
        }
        else
        {
            services.Configure<PubSubOptions>(configuration.GetSection(PubSubOptions.SectionName));
            services.AddBuildingBlocksMessaging();
        }

        if (configuration.GetValue("PubSub:EnableWorkers", true))
        {
            services.AddEventSubscriptionWorker<DiagnosticoFinalizadoData, DiagnosticoFinalizadoConsumer>(
                configuration["PubSub:Subscriptions:DiagnosticoFinalizado"] ?? SubscriptionDiagnosticoFinalizado);
            services.AddEventSubscriptionWorker<OrdemServicoFinalizadaData, OrdemServicoFinalizadaConsumer>(
                configuration["PubSub:Subscriptions:OrdemServicoFinalizada"] ?? SubscriptionOrdemServicoFinalizada);
            services.AddEventSubscriptionWorker<OrdemServicoDescartadaData, OrdemServicoDescartadaConsumer>(
                configuration["PubSub:Subscriptions:OrdemServicoDescartada"] ?? SubscriptionOrdemServicoDescartada);
        }

        return services;
    }
}
