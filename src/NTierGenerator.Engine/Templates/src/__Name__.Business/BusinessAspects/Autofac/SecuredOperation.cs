//#if Auth
using __Name__.Business.Constants;
using __Name__.Core.CrossCuttingConcerns.ExceptionHandling;
using __Name__.Core.Extensions;
using __Name__.Core.Utilities.Interceptors;
using __Name__.Core.Utilities.IoC;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Http;

namespace __Name__.Business.BusinessAspects.Autofac;

/// <summary>
/// Metodu çağıran kullanıcının JWT'deki yetkilerden (operation claim) en az birine sahip olmasını şart koşar.
/// Diğer aspect'lerden önce çalışır.
/// <para>Kullanım: <c>[SecuredOperation("admin,product.add")]</c></para>
/// </summary>
public sealed class SecuredOperation : MethodInterception
{
    private readonly string[] _roles;

    public SecuredOperation(string roles)
    {
        _roles = roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Priority = -100;
    }

    protected override void OnBefore(IInvocation invocation)
    {
        var user = ServiceTool.GetRequiredService<IHttpContextAccessor>().HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new AuthorizationDeniedException(Messages.AuthenticationRequired);
        }

        var userRoles = user.GetRoles();
        if (!_roles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
        {
            throw new AuthorizationDeniedException(Messages.AuthorizationDenied);
        }
    }
}
//#endif
