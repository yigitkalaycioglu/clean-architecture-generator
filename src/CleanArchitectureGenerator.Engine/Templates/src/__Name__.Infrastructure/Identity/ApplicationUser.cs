//#if LocalAuth
using Microsoft.AspNetCore.Identity;

namespace __Name__.Infrastructure.Identity;

/// <summary>
/// Oturum açabilen kullanıcı. Kimlik bilgileri (parola özeti, kilit durumu, iki adımlı doğrulama) Identity
/// tablolarında tutulur. Kullanıcıya ait iş verileri Domain'deki varlıklarda, kullanıcı kimliğiyle ilişkilendirilir.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.CreateVersion7();
        SecurityStamp = Guid.NewGuid().ToString();
    }
}
//#endif
