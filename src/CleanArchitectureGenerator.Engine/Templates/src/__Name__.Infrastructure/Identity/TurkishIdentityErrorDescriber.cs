//#if LocalAuth
using System.Globalization;
using Microsoft.AspNetCore.Identity;

namespace __Name__.Infrastructure.Identity;

/// <summary>Identity'nin kullanıcıya ulaşabilen hata mesajlarının Türkçesi.</summary>
internal sealed class TurkishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = string.Format(CultureInfo.InvariantCulture, "Parola en az {0} karakter olmalı.", length)
    };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
    {
        Code = nameof(PasswordRequiresUniqueChars),
        Description = string.Format(CultureInfo.InvariantCulture, "Parola en az {0} farklı karakter içermeli.", uniqueChars)
    };

    public override IdentityError InvalidEmail(string? email) => new()
    {
        Code = nameof(InvalidEmail),
        Description = "E-posta adresi geçersiz."
    };

    public override IdentityError InvalidUserName(string? userName) => new()
    {
        Code = nameof(InvalidUserName),
        Description = "E-posta adresi geçersiz."
    };

    public override IdentityError PasswordMismatch() => new()
    {
        Code = nameof(PasswordMismatch),
        Description = "Parola hatalı."
    };

    public override IdentityError InvalidToken() => new()
    {
        Code = nameof(InvalidToken),
        Description = "Bağlantı ya da kod geçersiz veya süresi dolmuş."
    };

    public override IdentityError DefaultError() => new()
    {
        Code = nameof(DefaultError),
        Description = "İşlem tamamlanamadı."
    };
}
//#endif
