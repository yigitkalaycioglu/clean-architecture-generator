namespace __Name__.Api.Common;

public static class ApiLimits
{
    /// <summary>İstek gövdesi en fazla 1 MB olabilir. Dosya yükleme gibi bir uç nokta eklerseniz yalnızca onda artırın.</summary>
    public const long MaxRequestBodySize = 1024 * 1024;
}

public static class RateLimitPolicies
{
    public const string Authentication = "authentication";
}

/// <summary>Hız sınırları ("RateLimiting" bölümü). Sınırlar istemci IP'si başına, her zaman penceresi için geçerlidir.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; set; } = 100;

    public int AuthenticationPermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}
