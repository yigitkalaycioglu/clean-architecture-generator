using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace __Name__.Api.Endpoints;

public static class EndpointMappings
{
    /// <summary>
    /// Sağlık kontrolleri anonimdir ve yalnızca "Healthy"/"Unhealthy" döner; sürüm, bağlantı cümlesi gibi ayrıntı
    /// vermez. /health/live süreç ayakta mı, /health/ready veritabanına ulaşılabiliyor mu sorusunu yanıtlar.
    /// </summary>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
        return app;
    }

    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
//#if LocalAuth
        app.MapAuthEndpoints();
//#endif
        app.MapAccountEndpoints();
//#if Sample
        app.MapTodoItemsEndpoints();
//#endif
        return app;
    }
}
