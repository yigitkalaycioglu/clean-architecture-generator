# __Name__

Clean Architecture ile düzenlenmiş, güvenlik öncelikli bir ASP.NET Core (.NET 10) Web API çözümü.
[Clean Architecture Oluşturucu](https://github.com/yigitkalaycioglu/clean-architecture-generator) v__GeneratorVersion__ ile oluşturuldu.

## Katmanlar

Bağımlılıklar yalnızca içe doğru akar. İç katmanlar dış katmanları bilmez; veritabanı, web çerçevesi ya da
kimlik sağlayıcı değişse bile iş kuralları değişmez. Bu kural `ArchitectureTests` ile her derlemede denetlenir.

```
Api ───────────────► Application ──► Domain
 │                        ▲
 └──► Infrastructure ─────┘
```

| Proje | Sorumluluk |
|---|---|
| `__Name__.Domain` | Varlıklar, iş kuralları, alan olayları ve `Result`/`Error` türleri. Hiçbir projeye ve pakete bağımlı değildir. |
| `__Name__.Application` | Kullanım senaryoları: her biri bir komut ya da sorgu, doğrulayıcısı ve işleyicisiyle tek dosyada. Dış dünya için arayüzler (`IApplicationDbContext`, `IUserContext`…) ve istek hattı (loglama, doğrulama). |
| `__Name__.Infrastructure` | Arayüzlerin uygulamaları: EF Core ile veritabanı, kimlik doğrulama, e-posta, denetim (audit) ve alan olayı kesicileri. |
| `__Name__.Api` | Minimal API uç noktaları, güvenlik ara katmanları, hata yönetimi, OpenAPI. Uygulamanın giriş noktası. |
<!--#if Tests-->
| `__Name__.UnitTests` | Domain kuralları, doğrulayıcılar ve istek hattı. |
| `__Name__.ArchitectureTests` | Katman bağımlılıkları ve kod kuralları. |
| `__Name__.IntegrationTests` | Uygulamanın tamamı bellekte, gerçek HTTP hattıyla: kimlik doğrulama, yetki, güvenlik başlıkları, hız sınırı. |
<!--#endif-->

İstekler uç noktadan Application'a `ISender` ile gönderilir ve her istek şu hattan geçer:
loglama → FluentValidation ile doğrulama → işleyici. Beklenen hatalar istisna değil `Result` olarak döner ve
API'de RFC 9457 ProblemDetails yanıtına çevrilir. MediatR gibi ücretli ya da harici bir kütüphane kullanılmaz;
istek gönderici birkaç sınıflık, okunabilir bir koddur (`Application/Messaging`).

## Klasör yapısı

```
__FolderTree__
```

Boş klasörlerdeki `.gitkeep` dosyaları yalnızca klasörün Git'te tutulması içindir; klasöre ilk dosyayı eklediğinizde silebilirsiniz.

## Başlarken

Gereksinimler: .NET 10 SDK ve __DbDisplayName__.
<!--#if SqlServer-->
Geliştirme bağlantı cümlesi Visual Studio ile gelen LocalDB'yi (`(localdb)\MSSQLLocalDB`) kullanır ve
`src/__Name__.Api/appsettings.Development.json` içindedir.
<!--#endif-->
<!--#if PostgreSql-->
Geliştirme bağlantı cümlesi `localhost:5432` üzerindeki PostgreSQL'i `postgres` kullanıcısıyla kullanır. Parola içerdiği
için yapılandırma dosyasında değil, user-secrets'tadır (aşağıya bakın).
<!--#endif-->
<!--#if Sqlite-->
SQLite için ayrı bir kurulum gerekmez; veritabanı dosyası (`__DatabaseName__.db`) uygulamanın çalışma klasöründe oluşur.
<!--#endif-->

```powershell
dotnet tool restore   # dotnet-ef aracını yükler (.config/dotnet-tools.json)
dotnet ef migrations add InitialCreate --project src/__Name__.Infrastructure --startup-project src/__Name__.Api --output-dir Persistence/Migrations
dotnet run --project src/__Name__.Api --launch-profile https
```

Uygulama Development ortamında açılırken bekleyen migration'ları uygular. Visual Studio'da başlangıç projesi
`__Name__.Api` olarak gelir, F5 yeterli; migration'ı Package Manager Console'dan eklemek için *Default project*
olarak `__Name__.Infrastructure` seçip `Add-Migration InitialCreate -OutputDir Persistence/Migrations` yazın.

- Swagger UI (yalnızca Development): https://localhost:__ApiHttpsPort__/swagger
- Sağlık kontrolleri: `/health/live` (süreç ayakta mı) ve `/health/ready` (veritabanına ulaşılıyor mu)
- Örnek istekler: `src/__Name__.Api/__Name__.Api.http`
<!--#if LocalAuth || PostgreSql-->

### Gizli değerler (user-secrets)

Gizli değerler hiçbir yapılandırma dosyasında ve Git deposunda bulunmaz. Çözümü oluşturan bilgisayarda bu
değerler `dotnet user-secrets` deposuna (`%APPDATA%\Microsoft\UserSecrets\__UserSecretsId__\secrets.json`) zaten
yazıldı. Depoyu başka bir bilgisayarda açtığınızda bir kez şunları çalıştırın:

```powershell
cd src/__Name__.Api
<!--#if LocalAuth-->
# 512 bit rastgele JWT imzalama anahtarı
dotnet user-secrets set "Jwt:SigningKey" ([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64)))
<!--#endif-->
<!--#if PostgreSql-->
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=__DatabaseName__;Username=postgres;Password=<parola>"
<!--#endif-->
```

<!--#if LocalAuth-->
Canlı ortamda aynı ayarlar ortam değişkenleriyle (`Jwt__SigningKey`, `ConnectionStrings__DefaultConnection`) ya da
<!--#else-->
Canlı ortamda aynı ayar ortam değişkeniyle (`ConnectionStrings__DefaultConnection`) ya da
<!--#endif-->
Azure Key Vault, AWS Secrets Manager gibi bir anahtar kasasıyla verilir. Ayar eksik ya da geçersizse uygulama
açılmaz ve hangi ayarın eksik olduğunu söyler.
<!--#endif-->

## Kimlik doğrulama
<!--#if LocalAuth-->

Kullanıcılar ASP.NET Core Identity ile bu API'de tutulur. API kısa ömürlü bir erişim token'ı (JWT, 10 dakika) ve
uzun ömürlü bir yenileme token'ı (14 gün) üretir:

- Erişim token'ı yanıt gövdesinde döner; istemci bellekte tutar ve `Authorization: Bearer <token>` başlığıyla gönderir.
- Yenileme token'ı JavaScript'in okuyamadığı bir çerezde (`HttpOnly; Secure; SameSite=Strict; Path=/api/auth`) durur.
  Her yenilemede değişir; kullanılmış bir token tekrar gelirse çalınmış sayılır ve o oturumun tüm token'ları iptal edilir.

| Uç nokta | Açıklama |
|---|---|
| `POST /api/auth/register` | Hesap açar, doğrulama e-postası gönderir. Adres kayıtlıysa da aynı yanıt döner. |
| `POST /api/auth/confirm-email` | E-postadaki `userId` ve `code` ile hesabı doğrular. |
| `POST /api/auth/login` | Giriş. İki adımlı doğrulama açıksa `twoFactorCode` ya da `recoveryCode` da gönderilir. |
| `POST /api/auth/refresh` | Çerezdeki yenileme token'ıyla yeni erişim token'ı. |
| `POST /api/auth/logout` | Oturumu kapatır, yenileme token'larını iptal eder. |
| `POST /api/auth/forgot-password`, `reset-password` | E-postayla parola sıfırlama. |
| `GET /api/account` | Hesap bilgileri. |
| `POST /api/account/change-password` | Parola değiştirir, tüm oturumları kapatır. |
| `POST /api/account/2fa/setup`, `enable`, `disable`, `recovery-codes` | Doğrulayıcı uygulamayla (TOTP) iki adımlı doğrulama. |

Development ortamında e-posta gönderilmez; doğrulama ve sıfırlama bağlantıları konsol loguna yazılır. Diğer
ortamlarda `Smtp` ve `Frontend:BaseUrl` ayarları zorunludur. E-postalardaki bağlantılar `Frontend:BaseUrl` adresindeki
istemci uygulamanızın `/confirm-email` ve `/reset-password` sayfalarına gider; bu sayfalar ilgili API uç noktasını çağırır.
<!--#else-->

Kullanıcılar harici bir OpenID Connect sağlayıcısında (Microsoft Entra ID, Auth0, Keycloak, Okta, Google…) tutulur;
API parola saklamaz, yalnızca sağlayıcının imzaladığı erişim token'larını doğrular. `appsettings.json` ya da ortam
değişkenleriyle iki ayar verin:

```json
"Authentication": {
  "Authority": "https://login.microsoftonline.com/<tenant-id>/v2.0",
  "Audience": "api://__Name__"
}
```

İmza anahtarları sağlayıcının keşif belgesinden otomatik alınır ve yenilenir. Yalnızca asimetrik imzalar (RS256,
PS256, ES256…) kabul edilir; yayıncı, hedef kitle ve süre her istekte doğrulanır. Kullanıcı kimliği token'daki `sub`
talebidir. Development'ta ayar boşsa uygulama açılır ama hiçbir token kabul edilmez; diğer ortamlarda https bir
`Authority` zorunludur.
<!--#endif-->

Tüm uç noktalar varsayılan olarak kimlik doğrulaması ister (fallback policy); anonim erişim yalnızca `AllowAnonymous`
ile açıkça verilir. Yetkilendirme için politika eklemek isterseniz `src/__Name__.Api/DependencyInjection.cs`
içindeki `AddAuthorizationBuilder()` çağrısına `AddPolicy` ekleyin ve uç noktada `RequireAuthorization("politika")` kullanın.

## Güvenlik

Uygulanan önlemlerin tamamı ve canlıya çıkış kontrol listesi [SECURITY.md](SECURITY.md) dosyasında. Özetle:
varsayılan olarak kapalı uç noktalar, kayıt sahipliği denetimi (başkasının kaydı 404), hız sınırı, güvenlik
başlıkları, gövde boyutu ve istek süresi sınırları, beklenmeyen JSON alanlarının reddi, iç ayrıntı sızdırmayan hata
yanıtları, depoda gizli değer bulunmaması ve bilinen açıklara karşı taranan, sürümü kilitli paketler.

## Yeni bir özellik eklemek
<!--#if Sample-->

`TodoItems` örneği her adımı gösterir. Örneğin `Order` için:
<!--#else-->

Örneğin `Order` için:
<!--#endif-->

1. **Domain**: `Orders/Order.cs` (`AuditableEntity`'den türer, durumu yalnızca metotlarla değişir) ve `Orders/OrderErrors.cs`.
2. **Application**: `IApplicationDbContext`'e `DbSet<Order> Orders { get; }`; `Orders/CreateOrder.cs` içinde komut,
   doğrulayıcı ve işleyici. İşleyici kaydı sorgularken her zaman kullanıcı kimliğiyle filtreler.
3. **Infrastructure**: `ApplicationDbContext`'e `DbSet<Order>` ve `Persistence/Configurations/OrderConfiguration.cs`.
4. **Api**: `Endpoints/OrdersEndpoints.cs` ve `EndpointMappings.MapApiEndpoints` içine bir satır.
5. Migration: `dotnet ef migrations add AddOrders --project src/__Name__.Infrastructure --startup-project src/__Name__.Api`

İşleyiciler, doğrulayıcılar ve alan olayı işleyicileri otomatik bulunur; DI kaydı yazmanız gerekmez.
<!--#if Tests-->

## Testler

```powershell
dotnet test
```

Entegrasyon testleri, seçilen veritabanından bağımsız olarak bellekte SQLite kullanır; harici bir servis gerekmez.
<!--#endif-->

## Paket ve derleme ayarları

- NuGet sürümleri yalnızca `Directory.Packages.props` içinde (Central Package Management); dolaylı paketler de sabitlenir.
- Çözülen paket sürümleri `packages.lock.json` dosyalarıyla kilitlenir; bu dosyaları depoya ekleyin. CI'da `CI=true` ile
  kilitten sapan geri yükleme hata verir.
- Paketler bilinen güvenlik açıklarına karşı taranır (NuGet Audit); açık bulunursa Release derlemesi durur.
- `nuget.config` paketleri yalnızca nuget.org'dan indirir (Package Source Mapping).
- .NET güvenlik analizörlerinin tümü açıktır; Release derlemesinde her uyarı hatadır. Kod stili `.editorconfig` içinde.
