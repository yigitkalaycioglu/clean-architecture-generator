# Clean Architecture Oluşturucu

Yeni bir ASP.NET Core Web API projesine başlarken Clean Architecture katmanlarını, klasörlerini, güvenlik
altyapısını ve testlerini her seferinde elle kurmak yerine tek tıkla oluşturan bir Windows uygulaması. Çözüm adını
ve birkaç seçeneği belirleyip butona basınca derlenmeye ve çalışmaya hazır bir .NET 10 çözümü çıkıyor: Domain,
Application, Infrastructure ve Api katmanları, kimlik doğrulama, örnek bir özellik ve birim, mimari ve entegrasyon
testleri.

Üretilen çözüm belirli bir iş alanına bağlı değil; aynı iskelet bir e-ticaret, iç sistem ya da mobil uygulama
arka ucu için kullanılabiliyor. Güvenlik varsayılan olarak açık geliyor: OWASP Top 10 ve ASVS başlıklarına göre
hazırlanan önlemler ve canlıya çıkış kontrol listesi çözümün `SECURITY.md` dosyasında.

C# ve .NET 10 ile yazıldı, arayüz WinForms. Sağdaki önizlemede üretilecek her dosya, diske yazılmadan önce
içeriğiyle birlikte görülebiliyor.

![Clean Architecture Oluşturucu ekran görüntüsü](docs/screenshot.png)

## Ne üretiyor

Varsayılan seçeneklerle `Firma.Proje` adı için:

```
Firma.Proje/
├── src/
│   ├── Firma.Proje.Domain/          varlıklar ve iş kuralları; hiçbir pakete bağımlı değil
│   │   ├── Common/                  Entity, AuditableEntity, IDomainEvent, Result, Error
│   │   └── TodoItems/               örnek varlık, hataları ve alan olayı
│   ├── Firma.Proje.Application/     kullanım senaryoları
│   │   ├── Abstractions/            IApplicationDbContext, IUserContext, IIdentityService, ISender…
│   │   ├── Behaviors/               loglama ve FluentValidation adımları
│   │   ├── Messaging/               bağımlılıksız CQRS gönderici ve alan olayı dağıtıcı
│   │   ├── Authentication/, Account/  kayıt, giriş, oturum, parola, iki adımlı doğrulama
│   │   └── TodoItems/               her dosyada bir komut ya da sorgu + doğrulayıcı + işleyici
│   ├── Firma.Proje.Infrastructure/  EF Core, Identity, JWT, e-posta kuyruğu, denetim kesicileri
│   └── Firma.Proje.Api/             minimal API uç noktaları, güvenlik ara katmanları, OpenAPI
├── tests/
│   ├── Firma.Proje.UnitTests/
│   ├── Firma.Proje.ArchitectureTests/   katman bağımlılıklarını derlenmiş kod üzerinden denetler
│   └── Firma.Proje.IntegrationTests/    uygulamanın tamamı bellekte, gerçek HTTP hattıyla
├── Directory.Build.props            ortak ayarlar, güvenlik analizörleri, NuGet Audit, kilit dosyaları
├── Directory.Packages.props         tüm NuGet sürümleri tek yerde
├── nuget.config                     yalnızca nuget.org (Package Source Mapping)
├── README.md, SECURITY.md
└── Firma.Proje.sln
```

Bağımlılıklar yalnızca içe doğru akıyor: `Api → Infrastructure → Application → Domain`. Bu kural belgede kalmıyor;
mimari testleri her derlemede denetliyor.

Üretilen kodda neler var:

- CQRS için MediatR yerine birkaç sınıflık, okunabilir bir gönderici. Her istek loglama ve FluentValidation
  adımlarından geçiyor; beklenen hatalar istisna değil `Result` olarak dönüyor ve API'de RFC 9457 ProblemDetails
  yanıtına çevriliyor. İşleyiciler ve doğrulayıcılar otomatik kaydediliyor.
- Zengin domain modeli: varlıkların durumu yalnızca kendi metotlarıyla değişiyor, kimlikler zaman sıralı GUID (v7),
  alan olayları kaydetme sırasında aynı işlem içinde işleyicilerine iletiliyor, oluşturma/değişiklik bilgisi
  sunucuda otomatik dolduruluyor.
- Yerleşik kimlik doğrulama (ASP.NET Core Identity): e-posta doğrulaması, 5 hatalı denemede 15 dakika kilit, TOTP ile
  iki adımlı doğrulama ve kurtarma kodları, parola sıfırlama. 10 dakikalık JWT erişim token'ı ve
  `HttpOnly; Secure; SameSite=Strict` çerezde duran, her kullanımda değişen yenileme token'ı; çalınmış bir token
  tekrar kullanılırsa oturumun tüm token'ları iptal ediliyor.
- Kullanıcı numaralandırmaya karşı: giriş, kayıt ve parola sıfırlama, adresin kayıtlı olup olmadığını ne mesajla ne
  de yanıt süresiyle ele veriyor (e-postalar arka planda kuyruktan gönderiliyor).
- Varsayılan olarak kapalı uç noktalar, kayıt sahipliği denetimi (başkasının kaydı 404), beklenmeyen JSON
  alanlarının reddi, IP başına ve giriş işlemlerinde ayrı hız sınırı, güvenlik başlıkları, gövde/süre/sayfa sınırları,
  iç ayrıntı sızdırmayan hata yanıtları.
- Gizli değerler depoda değil: JWT imzalama anahtarı (ve PostgreSQL'de bağlantı cümlesi) oluşturulurken
  `dotnet user-secrets` deposuna yazılıyor; canlıda eksikse uygulama açılmıyor.
- Tedarik zinciri: Central Package Management, dolaylı paketlerin sabitlenmesi, `packages.lock.json`, NuGet Audit
  (açık bulunursa Release derlemesi durur), yalnızca nuget.org'a izin veren `nuget.config`. .NET güvenlik
  analizörlerinin tümü açık; Release'te her uyarı hata.
- Çözümün kendi README'si (katmanlar, gerçek klasör ağacı, ilk migration ve çalıştırma komutları, yeni özellik
  ekleme adımları) ve SECURITY.md.

## Seçenekler

| Seçenek | Ne değişiyor |
|---|---|
| Çözüm adı | Kök namespace, klasör ve proje adları. Geçersiz adlar için öneri sunuyor ("Mağaza Yönetimi" → `MagazaYonetimi`); üretilen koddaki tür adlarıyla çakışan adları (`Firma.Result` gibi) reddediyor. |
| Veritabanı | SQL Server (LocalDB), PostgreSQL ya da SQLite: NuGet paketi, `UseXxx` çağrısı ve geliştirme bağlantı cümlesi. |
| Çözüm dosyası | Klasik `.sln` ya da yeni `.slnx`. |
| Kimlik doğrulama | Yerleşik (Identity + JWT, yukarıdaki tüm akışlarla) ya da harici bir OpenID Connect sağlayıcısı (Entra ID, Auth0, Keycloak…): API yalnızca token doğrular, yalnızca asimetrik imzaları kabul eder. |
| Örnek özellik | `TodoItems`: tüm katmanları, alan olayını ve sahiplik denetimini gösteren örnek. Kapalıyken iskelet boş geliyor. |
| Test projeleri | Birim, mimari ve entegrasyon testleri. Entegrasyon testleri seçilen veritabanından bağımsız olarak bellekte SQLite kullanıyor. |
| Oluşturduktan sonra | `git init`, `dotnet build` ve klasörü Gezgin'de açma. Derleme çıktısı uygulamadaki günlükte akıyor. |

## Kurulum ve kullanım

Gereksinim: Windows 10/11 ve .NET 10 SDK (uygulama için gereken .NET Desktop Runtime SDK ile geliyor; üretilen
çözümü derlemek için SDK zaten gerekli).

[Releases](https://github.com/yigitkalaycioglu/clean-architecture-generator/releases/latest) sayfasındaki zip'i açıp
`CleanArchitectureGenerator.exe`'yi çalıştırmanız yeterli. Kaynaktan çalıştırmak için:

```powershell
dotnet run --project src/CleanArchitectureGenerator.App
```

Tek dosyalık exe almak için `.\publish.ps1` (çıktı `publish\CleanArchitectureGenerator.exe`). .NET kurulu olmayan
bir bilgisayar için `.\publish.ps1 -SelfContained`.

Son kullanılan seçenekler `%LOCALAPPDATA%\CleanArchitectureGenerator\settings.json` dosyasında tutuluyor.

## Nasıl çalışıyor

- `src/CleanArchitectureGenerator.Engine/Templates` klasörü üretilen çözümün birebir kopyası; dosyalar klasör
  yollarıyla birlikte DLL'e gömülü kaynak olarak ekleniyor.
- Dosya içeriklerinde ve yollarında `__Name__` gibi token'lar değiştiriliyor. Seçeneğe bağlı kısımlar dosya
  türüne göre yorum satırı olarak yazılan bloklarla işaretli: `//#if LocalAuth` … `//#endif`,
  `<!--#if Sample-->`, `@*#if Sample*@`. `#elif`, `#else`, `!`, `&&`, `||` ve parantez destekleniyor;
  bilinmeyen bir sembol yazım hatası sayılıp hata veriyor. İçeriği tamamen koşullu olan dosyalar seçenek
  kapalıyken hiç üretilmiyor.
- `dot_gitignore` gibi adlar `.gitignore` olarak, `X.csproj.template` adları `X.csproj` olarak yazılıyor.
  `.template` son eki IDE'lerin şablon projelerini gerçek proje sanıp derlemesini engelliyor.
- Plan önce bellekte hazırlanıyor (önizleme bu planı gösteriyor), sonra diske yazılıyor. Çözüm dosyasındaki
  GUID'ler addan türetildiği için aynı seçenekler hep aynı çıktıyı veriyor. Api projesi çözüm dosyasına ilk sırada
  yazılıyor; Visual Studio'da F5 doğrudan API'yi çalıştırıyor.
- JWT imzalama anahtarı, user-secrets kimliği ve portlar her çözüm için rastgele. Gizli değerler çözüm klasörüne
  değil `%APPDATA%\Microsoft\UserSecrets\<kimlik>\secrets.json` dosyasına yazılıyor.
- Şablonları yeniden derlemeden değiştirmek için `Templates` klasörünü exe'nin yanına kopyalamak yeterli;
  uygulama varsa oradan okuyor.

```
src/CleanArchitectureGenerator.Engine          şablon işleme, çözüm üretimi, ad doğrulama (arayüzden bağımsız)
src/CleanArchitectureGenerator.App             WinForms arayüzü
tests/CleanArchitectureGenerator.Engine.Tests  xUnit testleri
```

## Testler

```powershell
dotnet test
```

Birim testleri tüm seçenek kombinasyonları için (3 veritabanı × 2 kimlik doğrulama × örnek/test açık-kapalı × 2 çözüm
biçimi = 48 kombinasyon) planı üretip çıktıda değiştirilmemiş token ya da şablon yönergesi kalmadığını, JSON ve XML
dosyalarının geçerli olduğunu, projelerin ve çözüm dosyasının tutarlı olduğunu ve hiçbir dosyaya gizli değer
yazılmadığını kontrol ediyor.

Üretilen çözümü gerçekten derleyen testler NuGet erişimi gerektirdiği ve birkaç dakika sürdüğü için ayrı:

```powershell
$env:CAG_BUILD_TESTS = "1"; dotnet test
```

Bunlar beş farklı senaryoyu sıfırdan üretip derliyor, tek bir derleme uyarısını bile hata sayıyor ve üretilen
testleri çalıştırıyor (her şey açıkken 52 test: kayıt/giriş akışları, hesap kilitleme, yenileme token'ı döngüsü ve
çalıntı tespiti, iki adımlı doğrulama, başkasının kaydına erişim, güvenlik başlıkları, hız sınırı, algoritma
karıştırma saldırısı…). Ayrıca üretilen API SQLite ile çalıştırılıp migration, Swagger UI ve uçtan uca akışlar
elle denendi.

## Bilinen sınırlamalar

- Yalnızca Windows'ta çalışıyor (WinForms). Üretim motoru platformdan bağımsız ama ayrı bir komut satırı aracı yok.
- Hedef framework .NET 10. Paket sürümleri şablondaki `Directory.Packages.props` dosyasına sabit yazılı
  (Ekim 2026 itibarıyla güncel); güncellemek için o dosyayı değiştirmek gerekiyor.
- Yalnızca API üretiyor; e-postadaki doğrulama ve parola sıfırlama bağlantılarını karşılayan sayfalar istemci
  uygulamada (SPA, mobil) yazılmalı.
- İlk migration üretilmiyor; çözümün README'sindeki `dotnet ef migrations add` komutuyla oluşturmak gerekiyor.
- Hız sınırı sunucu belleğinde tutuluyor; birden fazla sunucu çalıştırılıyorsa sınırlar sunucu başına geçerli olur.
  Data Protection anahtarlarının da paylaşılan bir yere alınması gerekir (SECURITY.md'deki kontrol listesinde).
- İki adımlı doğrulamanın kurtarma kodları, Identity'nin varsayılan deposunda olduğu gibi şifrelenmeden saklanıyor.
- Windows'un 260 karakter yol sınırı yüzünden çok derin klasörlerde oluşturulan çözümler derlenirken
  "dosya adı çok uzun" hatası alınabiliyor. Uygulama uzun yollarda uyarı veriyor.

Sürüm 1.x katmanlı (N-Tier) mimari üretiyordu; 2.0 ile tamamen Clean Architecture'a geçildi.

## Lisans

MIT
