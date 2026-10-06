//#if Auth
using __Name__.Core.Utilities.Security.JWT;
//#endif
using __Name__.DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace __Name__.Business;

/// <summary>
/// İş katmanının ve ihtiyaç duyduğu alt katmanların Microsoft DI kayıtları.
/// Manager sınıfları aspect desteği için ayrıca Autofac modülünde (AutofacBusinessModule) kaydedilir.
/// </summary>
public static class BusinessServiceRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataAccessServices(configuration);
//#if Auth

        services.Configure<TokenOptions>(configuration.GetSection(TokenOptions.SectionName));
        services.AddSingleton<ITokenHelper, JwtHelper>();
//#endif

        return services;
    }
}
