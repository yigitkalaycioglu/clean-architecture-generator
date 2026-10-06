using __Name__.Core.Utilities.Interceptors;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using Module = Autofac.Module;

namespace __Name__.Business.DependencyResolvers.Autofac;

/// <summary>
/// İş katmanı servislerini Autofac ile kaydeder ve aspect'lerin (doğrulama, cache, performans...)
/// çalışması için arayüz proxy'lerini (Castle DynamicProxy) etkinleştirir.
/// </summary>
public class AutofacBusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Konvansiyon: adı "Manager" ile biten her sınıf uyguladığı arayüzlerle kaydedilir
        // (ProductManager -> IProductService). Yeni bir servis eklediğinizde ayrıca kayıt yapmanız gerekmez.
        builder.RegisterAssemblyTypes(ThisAssembly)
            .Where(type => type.Name.EndsWith("Manager", StringComparison.Ordinal))
            .AsImplementedInterfaces()
            .EnableInterfaceInterceptors(new ProxyGenerationOptions { Selector = new AspectInterceptorSelector() })
            .InstancePerLifetimeScope();
    }
}
