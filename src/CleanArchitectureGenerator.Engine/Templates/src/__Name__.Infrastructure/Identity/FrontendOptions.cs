//#if LocalAuth
using Microsoft.AspNetCore.WebUtilities;

namespace __Name__.Infrastructure.Identity;

/// <summary>
/// E-postalardaki bağlantıların açılacağı istemci uygulama ("Frontend" bölümü). Bağlantılar her zaman bu
/// sabit adrese gider; istekteki Host başlığından üretilmez (host header injection ile bağlantı çalınamaz).
/// </summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>İstemci uygulamanın kök adresi, ör. https://app.ornek.com</summary>
    public string BaseUrl { get; set; } = string.Empty;

    internal string BuildLink(string path, IDictionary<string, string?> query)
    {
        var baseUri = new Uri(BaseUrl.EndsWith('/') ? BaseUrl : BaseUrl + "/");
        return QueryHelpers.AddQueryString(new Uri(baseUri, path).ToString(), query);
    }
}
//#endif
