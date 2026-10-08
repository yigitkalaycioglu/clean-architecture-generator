namespace __Name__.Domain.Common;

/// <summary>Hatanın türü; API katmanı bunu HTTP durum koduna çevirir (ör. NotFound -> 404).</summary>
public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden
}

/// <summary>
/// Beklenen (iş kuralı kaynaklı) bir hata. İstisna fırlatmak yerine <see cref="Result"/> ile döndürülür;
/// istisnalar yalnızca beklenmeyen durumlar içindir.
/// </summary>
/// <param name="Code">İstemcinin karşılaştırabileceği sabit kod, ör. "TodoItem.NotFound".</param>
/// <param name="Description">Kullanıcıya gösterilebilecek açıklama. İç ayrıntı (SQL, yığın izi) içermemelidir.</param>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>Doğrulama hatalarında alan adına göre hata mesajları.</summary>
    public IReadOnlyDictionary<string, string[]>? Details { get; init; }

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error Validation(IReadOnlyDictionary<string, string[]> details) =>
        new("Validation", "Bir ya da daha fazla alan geçersiz.", ErrorType.Validation) { Details = details };

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
}
