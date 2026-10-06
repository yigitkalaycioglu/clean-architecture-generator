using Microsoft.Extensions.DependencyInjection;

namespace __Name__.Core.Utilities.IoC;

/// <summary>
/// Core katmanındaki servis kayıt modüllerinin sözleşmesi.
/// </summary>
public interface ICoreModule
{
    void Load(IServiceCollection services);
}
