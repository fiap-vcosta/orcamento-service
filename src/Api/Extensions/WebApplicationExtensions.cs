using BuildingBlocks.Http.Middleware;

namespace Api.Extensions;

public static class WebApplicationExtensions
{
    public static void UseApiConfiguration(this WebApplication app)
    {
        app.UseMiddleware<RequestIdMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }
}
