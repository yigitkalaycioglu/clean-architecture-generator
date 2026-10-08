namespace __Name__.Application.Account;

//#if LocalAuth
public sealed record AccountResponse(string Id, string Email, bool EmailConfirmed, bool TwoFactorEnabled, int RecoveryCodesLeft);

/// <param name="SharedKey">Doğrulayıcı uygulamaya elle girilecek anahtar.</param>
/// <param name="AuthenticatorUri">QR kod olarak gösterilecek otpauth:// adresi.</param>
public sealed record TwoFactorSetupResponse(string SharedKey, string AuthenticatorUri)
{
    // Gizli anahtar içerdiği için ToString değerleri göstermez.
    public override string ToString() => nameof(TwoFactorSetupResponse);
}

/// <summary>Kurtarma kodları yalnızca bu yanıtta bir kez gösterilir; her biri tek kullanımlıktır.</summary>
public sealed record RecoveryCodesResponse(IReadOnlyCollection<string> RecoveryCodes)
{
    public override string ToString() => nameof(RecoveryCodesResponse);
}
//#else
/// <summary>
/// Kullanıcı bilgileri (ad, e-posta…) harici kimlik sağlayıcıda tutulur; bu API yalnızca kimliği bilir.
/// </summary>
public sealed record AccountResponse(string Id);
//#endif
