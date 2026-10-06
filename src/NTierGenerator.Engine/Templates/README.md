# __Name__

Katmanlı (N-Tier) mimariyle düzenlenmiş bir ASP.NET Core (.NET 10) çözümü.
[N-Tier Mimari Oluşturucu](https://github.com/yigitkalaycioglu/ntier-generator) v__GeneratorVersion__ ile oluşturuldu.

## Katmanlar

Bağımlılıklar tek yöne akar; her katman yalnızca altındaki katmanları tanır:

```
<!--#if WebApi && WebUi-->
WebAPI, WebUI ──► Business ──► DataAccess ──► Entities ──► Core
<!--#elif WebApi-->
WebAPI ──► Business ──► DataAccess ──► Entities ──► Core
<!--#else-->
WebUI ──► Business ──► DataAccess ──► Entities ──► Core
<!--#endif-->
```

| Proje | Sorumluluk |
|---|---|
| `__Name__.Core` | Projeden bağımsız altyapı: generic repository, `IResult` sonuç tipleri, iş kuralı yardımcısı, aspect'ler (AOP), cache, doğrulama ve merkezi hata yönetimi. Başka projelere olduğu gibi taşınabilir. |
| `__Name__.Entities` | Veritabanı varlıkları (`Concrete`) ve katmanlar arasında veri taşıyan nesneler (`DTOs`). |
| `__Name__.DataAccess` | EF Core ile veri erişimi: `Abstract` altında Dal arayüzleri, `Concrete/EntityFramework` altında uygulamaları, DbContext ve tablo ayarları (`Configurations`). |
| `__Name__.Business` | İş kuralları: `Abstract` altında servis arayüzleri, `Concrete` altında manager'lar, FluentValidation doğrulayıcıları, mesajlar ve Autofac modülü. |
<!--#if WebApi-->
| `__Name__.WebAPI` | REST API. Controller'lar yalnızca Business servislerini kullanır. OpenAPI + Swagger UI, merkezi hata yönetimi, CORS ve `/health` uç noktası. |
<!--#endif-->
<!--#if WebUi-->
| `__Name__.WebUI` | ASP.NET Core MVC arayüzü (Bootstrap 5). Controller'lar yalnızca Business servislerini kullanır. |
<!--#endif-->
<!--#if Tests-->
| `__Name__.Tests` | xUnit ve Moq ile birim testleri. |
<!--#endif-->

## Klasör yapısı

```
__FolderTree__
```

Boş klasörlerdeki `.gitkeep` dosyaları yalnızca klasörün Git'te tutulması içindir; klasöre ilk dosyayı eklediğinizde silebilirsiniz.

## Başlarken

Gereksinimler: .NET 10 SDK ve __DbDisplayName__.
<!--#if SqlServer-->
Bağlantı cümlesi Visual Studio ile gelen LocalDB'yi (`(localdb)\MSSQLLocalDB`) kullanır.
<!--#endif-->
<!--#if PostgreSql-->
Bağlantı cümlesi `localhost:5432` üzerindeki PostgreSQL'i `postgres/postgres` kullanıcısıyla kullanır.
<!--#endif-->
<!--#if Sqlite-->
SQLite için ayrı bir kurulum gerekmez; veritabanı dosyası (`__DatabaseName__.db`) uygulamanın çalışma klasöründe oluşur.
<!--#endif-->
Bağlantı cümlesini `src/__Name__.__StartupProject__/appsettings.json` içindeki `ConnectionStrings:DefaultConnection` ayarından değiştirebilirsiniz.

```powershell
dotnet tool restore   # dotnet-ef aracını yükler (.config/dotnet-tools.json)
dotnet build
dotnet ef migrations add InitialCreate --project src/__Name__.DataAccess --startup-project src/__Name__.__StartupProject__
dotnet run --project src/__Name__.__StartupProject__
```

Uygulama Development ortamında açılırken bekleyen migration'ları uygular; veritabanı yoksa oluşturur.
Visual Studio kullanıyorsanız Package Manager Console'da *Default project* olarak `__Name__.DataAccess`,
başlangıç projesi olarak `__Name__.__StartupProject__` seçiliyken `Add-Migration InitialCreate` yazmanız yeterli.

<!--#if WebApi-->
- Swagger UI: http://localhost:__ApiHttpPort__/swagger
- Sağlık kontrolü: http://localhost:__ApiHttpPort__/health
- Örnek istekler: `src/__Name__.WebAPI/__Name__.WebAPI.http`
<!--#endif-->
<!--#if WebUi-->
- MVC arayüzü: http://localhost:__UiHttpPort__
<!--#endif-->
<!--#if Auth-->

## Kimlik doğrulama (JWT)

1. `POST /api/auth/register` ile kayıt olun ya da `POST /api/auth/login` ile giriş yapın; yanıtta `token` döner.
2. Swagger UI'daki **Authorize** butonuna token'ı yapıştırın (ya da isteklere `Authorization: Bearer <token>` başlığı ekleyin).
3. Korumalı metotlar `[SecuredOperation("admin")]` ile işaretlidir. Yetkiler `OperationClaims` tablosunda tutulur ve
   kullanıcılara `UserOperationClaims` tablosuyla atanır. Başlangıçta `admin` (Id 1) yetkisi hazır gelir.

İlk kullanıcıya (Id 1) admin yetkisi vermek için:

<!--#if PostgreSql-->
```sql
INSERT INTO "UserOperationClaims" ("UserId", "OperationClaimId", "CreatedDate") VALUES (1, 1, now());
```
<!--#else-->
```sql
INSERT INTO UserOperationClaims (UserId, OperationClaimId, CreatedDate) VALUES (1, 1, CURRENT_TIMESTAMP);
```
<!--#endif-->

Yetkiler token'a giriş anında yazılır; yetki verdikten sonra yeniden giriş yapın.

JWT imza anahtarı bu çözüme özel olarak rastgele üretildi ve `appsettings.json` içinde. Canlı ortamda anahtarı
dosyada tutmayın; `TokenOptions__SecurityKey` ortam değişkeni ya da `dotnet user-secrets` ile verin.
<!--#endif-->

## Yeni bir özellik eklemek

Örneğin `Order` için, her adım bir katmana karşılık gelir:

1. **Entities**: `Concrete/Order.cs` (`BaseEntity`'den türer).
2. **DataAccess**: `Abstract/IOrderDal.cs` (`IEntityRepository<Order>`), `Concrete/EntityFramework/EfOrderDal.cs`
   (`EfEntityRepositoryBase<Order, __ContextName__>`), `Configurations/OrderConfiguration.cs` ve DbContext'e `DbSet<Order>`.
3. **Business**: `Abstract/IOrderService.cs`, `Concrete/OrderManager.cs`, `ValidationRules/FluentValidation/OrderValidator.cs`,
   mesajlar için `Constants/Messages.cs`.
<!--#if WebApi-->
4. **WebAPI**: `Controllers/OrdersController.cs`, constructor'dan `IOrderService` alır.
<!--#else-->
4. **WebUI**: `Controllers/OrdersController.cs` ve `Views/Orders/`, constructor'dan `IOrderService` alır.
<!--#endif-->
5. Migration: `dotnet ef migrations add AddOrder --project src/__Name__.DataAccess --startup-project src/__Name__.__StartupProject__`

Adı `Dal` ile biten sınıflar DataAccess'te, adı `Manager` ile biten sınıflar Business'ta otomatik kaydedilir;
DI için ayrıca bir şey yazmanız gerekmez.

## Aspect'ler (AOP)

İş metotlarına attribute olarak eklenir; metot gövdesinde yalnızca iş mantığı kalır.

| Attribute | Ne yapar |
|---|---|
| `[ValidationAspect(typeof(ProductValidator))]` | Parametreleri FluentValidation ile doğrular; geçersizse API 400 döner. |
| `[CacheAspect(duration: 10)]` | Sonucu 10 dakika cache'ler. Başarısız sonuçlar cache'lenmez. |
| `[CacheRemoveAspect("IProductService.Get")]` | Metot başarıyla bitince anahtarı desene uyan cache kayıtlarını siler. |
| `[PerformanceAspect(3)]` | 3 saniyeyi aşan çağrıları uyarı olarak loglar. |
| `[TransactionScopeAspect]` | Metodu tek bir transaction'da çalıştırır; hata ya da başarısız sonuçta değişiklikleri geri alır. |
<!--#if Auth-->
| `[SecuredOperation("admin,product.add")]` | Kullanıcının bu yetkilerden birine sahip olmasını şart koşar; yoksa 401/403 döner. |
<!--#endif-->

Aspect'ler Autofac'in arayüz proxy'leriyle (Castle DynamicProxy) çalışır, yani yalnızca DI'dan alınan servis arayüzü
üzerinden yapılan çağrılarda devreye girer. Async metotlar desteklenir: cache, transaction ve süre ölçümü görev
tamamlandıktan sonra yapılır.
<!--#if Tests-->

## Testler

```powershell
dotnet test
```
<!--#endif-->

## Paket ve derleme ayarları

- NuGet sürümleri yalnızca `Directory.Packages.props` içinde tutulur (Central Package Management); `.csproj` dosyalarında sürüm yazılmaz.
- Hedef framework, nullable ve diğer ortak ayarlar `Directory.Build.props` içinde.
- Kod stili kuralları `.editorconfig` içinde.
