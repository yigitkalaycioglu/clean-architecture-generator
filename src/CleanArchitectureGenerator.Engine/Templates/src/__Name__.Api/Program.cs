using __Name__.Api;
using __Name__.Api.Common;
using __Name__.Api.Endpoints;
using __Name__.Application;
using __Name__.Infrastructure;
using __Name__.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    // Sunucu yazılımı yanıt başlığında ilan edilmez; çok büyük gövdelerle bellek tüketilemez.
    kestrel.AddServerHeader = false;
    kestrel.Limits.MaxRequestBodySize = ApiLimits.MaxRequestBodySize;
});

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

// ---------------------------------------------------------------- HTTP istek hattı (sıra önemlidir)
app.UseForwardedHeaders();
app.UseSecurityHeaders(app.Environment);
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // API dokümanı ve Swagger UI yalnızca geliştirmede açılır; canlıda saldırı yüzeyini büyütmez.
    app.MapOpenApi().AllowAnonymous();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "__Name__ API v1"));
    await app.Services.ApplyMigrationsAsync();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestTimeouts();

app.MapHealthEndpoints();
app.MapApiEndpoints();

await app.RunAsync();
