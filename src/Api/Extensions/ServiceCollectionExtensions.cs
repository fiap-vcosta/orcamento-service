using System.Text.Json.Serialization;
using Api.Services;
using BuildingBlocks.Http.ProblemDetails;
using Domain.Exceptions;
using Infrastructure.DependencyInjection;

namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);

        services.AddSingleton<IWebhookDispatcher, WebhookDispatcher>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(
                typeof(Application.UseCases.Webhook.Commands.ProcessarWebhookMercadoPago.ProcessarWebhookMercadoPagoCommand).Assembly));

        services.AddBuildingBlocksProblemDetails(options =>
        {
            options.Map<DomainNotFoundException>(StatusCodes.Status404NotFound, "Not Found");
            options.Map<KeyNotFoundException>(StatusCodes.Status404NotFound, "Not Found");
            options.Map<BusinessRuleException>(StatusCodes.Status400BadRequest, "Business Rule Violation");
            options.Map<ArgumentException>(StatusCodes.Status400BadRequest, "Bad Request");
        });

        services.AddControllers(options =>
            {
                options.Filters.Add<ProblemDetailsExceptionFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
    }
}
