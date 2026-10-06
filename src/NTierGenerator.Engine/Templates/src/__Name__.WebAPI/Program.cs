using System.Text.Json.Serialization;
using __Name__.Business;
using __Name__.Business.DependencyResolvers.Autofac;
using __Name__.Core.CrossCuttingConcerns.ExceptionHandling;
using __Name__.Core.DependencyResolvers;
using __Name__.Core.Extensions;
using __Name__.Core.Utilities.IoC;
//#if Auth
using __Name__.Core.Utilities.Security.Encryption;
using __Name__.Core.Utilities.Security.JWT;
//#endif
using __Name__.DataAccess;
//#if Auth
using __Name__.WebAPI.OpenApi;
//#endif
using Autofac;
using Autofac.Extensions.DependencyInjection;
//#if Auth
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
//#endif

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Servisler
// Autofac: Business katmanındaki manager'ları aspect (AOP) desteğiyle kaydeder.
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container => container.RegisterModule(new AutofacBusinessModule()));

builder.Services.AddBusinessServices(builder.Configuration);
builder.Services.AddDependencyResolvers(new CoreModule());

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
//#if Auth
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
//#else
builder.Services.AddOpenApi();
//#endif
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
//#if Auth

var tokenOptions = builder.Configuration.GetSection(TokenOptions.SectionName).Get<TokenOptions>()
    ?? throw new InvalidOperationException($"'{TokenOptions.SectionName}' ayarı bulunamadı (appsettings.json).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = tokenOptions.Issuer,
            ValidAudience = tokenOptions.Audience,
            IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
//#endif
builder.Services.AddAuthorization();

var app = builder.Build();

// Aspect'ler attribute olduğu için servislere ServiceTool üzerinden erişir.
ServiceTool.Initialize(app.Services);

// ---------------------------------------------------------------- HTTP istek hattı
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "__Name__ API v1"));

    // Bekleyen migration'lar varsa veritabanını günceller (yalnızca geliştirme ortamında).
    await app.Services.ApplyDatabaseMigrationsAsync();
}

app.UseHttpsRedirection();
app.UseCors();
//#if Auth
app.UseAuthentication();
//#endif
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
