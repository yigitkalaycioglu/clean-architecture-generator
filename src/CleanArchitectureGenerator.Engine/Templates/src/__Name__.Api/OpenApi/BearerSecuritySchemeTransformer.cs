using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace __Name__.Api.OpenApi;

/// <summary>
/// OpenAPI dokümanına JWT Bearer güvenlik şemasını ekler; Swagger UI'daki "Authorize" butonuna erişim token'ı
/// yapıştırılarak korumalı uç noktalar denenebilir.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.All(scheme => scheme.Name != SchemeName))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
//#if LocalAuth
            Description = "POST /api/auth/login yanıtındaki accessToken değerini girin."
//#else
            Description = "Kimlik sağlayıcınızdan bu API için alınan erişim token'ını girin."
//#endif
        };

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = []
        };

        var operations = document.Paths.Values
            .Where(path => path.Operations is not null)
            .SelectMany(path => path.Operations!.Values);

        foreach (var operation in operations)
        {
            operation.Security ??= [];
            operation.Security.Add(requirement);
        }
    }
}
