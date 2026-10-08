//#if LocalAuth
using __Name__.Domain.Common;

namespace __Name__.Application.Abstractions.Authentication;

/// <summary>
/// Kimlik doğrulama hataları. Mesajlar bilerek geneldir: "böyle bir kullanıcı yok", "parola yanlış" ya da
/// "hesap kilitli" ayrımı yapılmaz; böylece bir e-posta adresinin kayıtlı olup olmadığı öğrenilemez.
/// </summary>
public static class IdentityErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized("Auth.InvalidCredentials", "E-posta ya da parola hatalı.");

    public static readonly Error TwoFactorRequired = Error.Unauthorized("Auth.TwoFactorRequired", "İki adımlı doğrulama kodu gerekli.");

    public static readonly Error EmailNotConfirmed = Error.Forbidden("Auth.EmailNotConfirmed", "E-posta adresiniz henüz doğrulanmadı.");

    public static readonly Error InvalidRefreshToken = Error.Unauthorized("Auth.InvalidRefreshToken", "Oturumun süresi doldu, yeniden giriş yapın.");

    public static readonly Error InvalidCode = Error.Validation("Auth.InvalidCode", "Bağlantı ya da kod geçersiz veya süresi dolmuş.");

    public static readonly Error InvalidTwoFactorCode = Error.Unauthorized("Auth.InvalidTwoFactorCode", "Doğrulama kodu hatalı.");

    public static readonly Error InvalidVerificationCode = Error.Validation("Auth.InvalidVerificationCode", "Doğrulama kodu hatalı.");

    public static readonly Error PasswordMismatch = Error.Validation("Auth.PasswordMismatch", "Parola hatalı.");

    public static readonly Error TwoFactorAlreadyEnabled = Error.Conflict("Auth.TwoFactorAlreadyEnabled", "İki adımlı doğrulama zaten açık.");

    public static readonly Error TwoFactorNotEnabled = Error.Conflict("Auth.TwoFactorNotEnabled", "İki adımlı doğrulama açık değil.");

    public static readonly Error UserNotFound = Error.NotFound("Auth.UserNotFound", "Kullanıcı bulunamadı.");
}
//#endif
