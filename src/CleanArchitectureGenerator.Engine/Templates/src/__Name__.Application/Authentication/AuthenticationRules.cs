//#if LocalAuth
using FluentValidation;

namespace __Name__.Application.Authentication;

/// <summary>
/// Hesap alanlarının ortak kuralları. Parola politikası NIST SP 800-63B'yi izler: karmaşıklık kuralları
/// (büyük harf, sembol…) yerine uzunluk istenir; sık kullanılan parolalar Infrastructure'da ayrıca reddedilir.
/// </summary>
public static class AuthenticationRules
{
    public const int PasswordMinLength = 12;

    /// <summary>Çok uzun parolalarla özetleme maliyetini şişirerek yapılan kaynak tüketimini önler.</summary>
    public const int PasswordMaxLength = 128;

    public const int EmailMaxLength = 256;

    public const int CodeMaxLength = 2048;

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(EmailMaxLength).EmailAddress();

    public static IRuleBuilderOptions<T, string> ValidNewPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MinimumLength(PasswordMinLength).MaximumLength(PasswordMaxLength);

    /// <summary>Var olan parolayı doğrularken yalnızca biçim denetlenir; uzunluk politikası burada uygulanmaz.</summary>
    public static IRuleBuilderOptions<T, string> ValidCurrentPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(PasswordMaxLength);

    public static IRuleBuilderOptions<T, string> ValidCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(CodeMaxLength);
}
//#endif
