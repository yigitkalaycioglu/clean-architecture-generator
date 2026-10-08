# Güvenlik

Bu belge __Name__ çözümünde hazır gelen güvenlik önlemlerini ve canlıya çıkmadan önce yapılması gerekenleri
listeler. Önlemler OWASP Top 10 (2021) ve OWASP ASVS 4.0 başlıklarına göre gruplanmıştır.

## Hazır gelen önlemler

### Erişim denetimi (A01)

- Tüm uç noktalar varsayılan olarak kimlik doğrulaması ister (fallback policy); anonim erişim yalnızca açıkça verilir.
- Kullanıcı kimliği yalnızca doğrulanmış token'dan okunur (`IUserContext`); gövdede ya da adreste gelen bir kimliğe güvenilmez.
<!--#if Sample-->
- Kayıt sahipliği her sorguda veritabanı filtresiyle denetlenir. Başka bir kullanıcının kaydı `404` döner; kaydın var olduğu bile anlaşılmaz (IDOR koruması).
<!--#endif-->
- JSON gövdesinde beklenmeyen alanlar reddedilir (`UnmappedMemberHandling.Disallow`): sahiplik ya da denetim alanları istemciden atanamaz (mass assignment).
- CORS yalnızca `Cors:AllowedOrigins` listesindeki kaynaklara açıktır; liste boşsa tarayıcıdan başka kaynak erişemez.

### Kriptografi ve gizli değerler (A02)

- Hiçbir gizli değer yapılandırma dosyalarında ya da Git deposunda bulunmaz: geliştirmede user-secrets, canlıda ortam değişkeni ya da anahtar kasası.
- Eksik ya da zayıf ayarlarla uygulama açılmaz (`ValidateOnStart`).
- Canlıda HSTS (1 yıl, alt alan adları dahil) ve HTTPS yönlendirmesi açıktır.
<!--#if LocalAuth-->
- Parolalar PBKDF2-HMAC-SHA512 ile 210.000 tekrarla özetlenir (OWASP Password Storage Cheat Sheet).
- JWT imzalama anahtarı en az 256 bit olmalıdır; yalnızca HS256 kabul edilir (`alg: none` ve algoritma karıştırma reddedilir).
- Yenileme token'ları 512 bit rastgeledir ve veritabanında yalnızca SHA-256 özetleri saklanır.
<!--#else-->
- Yalnızca asimetrik imzalı token'lar (RS/PS/ES) kabul edilir; `alg: none` ve HMAC ile algoritma karıştırma reddedilir. Canlıda sağlayıcı adresi https olmak zorundadır.
<!--#endif-->

### Enjeksiyon (A03)

- Tüm veritabanı erişimi EF Core LINQ sorgularıyla, parametreli yapılır; elle SQL birleştirilmez.
- API yalnızca JSON döndürür ve katı bir Content-Security-Policy (`default-src 'none'`) gönderir; yanıtlar bir sayfada betik olarak çalıştırılamaz.
- Girdiler işleyiciye ulaşmadan FluentValidation ile denetlenir; varlıklar kendi kurallarını ayrıca korur.
<!--#if LocalAuth-->
- E-postalar düz metindir; bağlantılar istekteki Host başlığından değil, sabit `Frontend:BaseUrl` ayarından üretilir.
<!--#endif-->

### Güvenli tasarım ve yapılandırma (A04, A05)

- Güvenlik başlıkları: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy`, `Cross-Origin-Resource-Policy`, `Cache-Control: no-store`.
- `Server` başlığı gönderilmez; Swagger UI ve OpenAPI dokümanı yalnızca Development'ta açılır.
- Hata yanıtları RFC 9457 ProblemDetails biçimindedir: yığın izi, SQL ya da iç mesaj içermez, yalnızca loglarla eşleştirmek için `traceId` taşır.
- Kaynak tüketimine karşı sınırlar: istek gövdesi 1 MB, istek süresi 30 sn, JSON derinliği 32, sayfa boyutu 100, sayfa derinliği 10.000.
<!--#if LocalAuth-->
- Hız sınırı: IP başına dakikada 100 istek; giriş, kayıt ve parola işlemlerinde dakikada 10. Sınır aşılınca `429` ve `Retry-After`.
<!--#else-->
- Hız sınırı: IP başına dakikada 100 istek. Sınır aşılınca `429` ve `Retry-After`.
<!--#endif-->
- `X-Forwarded-For` başlığı yalnızca `ForwardedHeaders:KnownProxies` listesindeki vekil sunuculardan kabul edilir; istemci sahte IP ile hız sınırını aşamaz.

### Bileşenler (A06)

- Paket sürümleri merkezi olarak yönetilir, dolaylı paketler dahil sabitlenir ve `packages.lock.json` ile kilitlenir.
- NuGet Audit tüm paketleri bilinen açıklara karşı tarar; açık bulunursa Release derlemesi durur.
- `nuget.config` paketleri yalnızca nuget.org'dan indirir (Package Source Mapping, dependency confusion koruması).
- .NET güvenlik analizörlerinin tümü açıktır; Release derlemesinde her uyarı hatadır.

### Kimlik doğrulama (A07)
<!--#if LocalAuth-->

- Parola politikası NIST SP 800-63B'yi izler: en az 12, en fazla 128 karakter; sık kullanılan ve sızdırılmış parolalar ile e-posta adresini içeren parolalar reddedilir.
- 5 hatalı denemeden sonra hesap 15 dakika kilitlenir; hatalı iki adımlı doğrulama kodları da sayılır.
- Hata mesajları geneldir ve yanıt süresi sabitlenir: bir e-posta adresinin kayıtlı olup olmadığı giriş, kayıt ya da parola sıfırlama ile öğrenilemez.
- Giriş için doğrulanmış e-posta şarttır. E-posta doğrulama ve parola sıfırlama bağlantıları 2 saat geçerlidir.
- İsteğe bağlı TOTP ile iki adımlı doğrulama ve tek kullanımlık kurtarma kodları.
- Erişim token'ı 10 dakika geçerlidir. Yenileme token'ı `HttpOnly; Secure; SameSite=Strict` bir çerezdedir, her kullanımda değişir; çalınmış bir token tekrar kullanılırsa oturumun tüm token'ları iptal edilir.
- Parola değiştirme ve sıfırlama tüm açık oturumları kapatır; iki adımlı doğrulamayı kapatmak ve kurtarma kodu üretmek parola ister.
<!--#else-->

- Kimlik bilgileri harici sağlayıcıdadır; parola politikası, hesap kilitleme ve çok faktörlü doğrulama sağlayıcıda yapılandırılır.
- Her istekte imza, yayıncı, hedef kitle (`aud`) ve süre doğrulanır; saat kayması toleransı 30 saniyedir.
<!--#endif-->

### Bütünlük, loglama ve izleme (A08, A09)

- İstek içerikleri (parola, token, kişisel veri) loglanmaz; istekler yalnızca adı, sonucu ve süresiyle loglanır.
<!--#if LocalAuth-->
- Token ve parola taşıyan türler `ToString` ile içeriklerini göstermez; yanlışlıkla loglansalar bile değerler sızmaz.
<!--#endif-->
- Kayıtların oluşturma ve değişiklik zamanı ile kullanıcısı sunucuda, otomatik doldurulur.
<!--#if LocalAuth-->
- Kilitli hesaba giriş denemesi, çalınmış yenileme token'ı kullanımı ve iki adımlı doğrulamanın kapatılması uyarı olarak loglanır.
<!--#endif-->

## Canlıya çıkmadan önce

<!--#if LocalAuth-->
- [ ] Gizli değerleri bir anahtar kasasına ya da ortam değişkenlerine taşıyın: `Jwt__SigningKey`, `Smtp__Password`, `ConnectionStrings__DefaultConnection`.
- [ ] `Smtp` ayarlarını (587 numaralı port, TLS) ve `Frontend:BaseUrl` adresini girin.
- [ ] Data Protection anahtarlarını kalıcı ve paylaşılan bir yere alın (birden fazla sunucu ya da konteyner varsa); aksi halde her yeniden başlatmada e-posta bağlantıları geçersiz olur.
<!--#else-->
- [ ] Bağlantı cümlesini bir anahtar kasasına ya da ortam değişkenine taşıyın: `ConnectionStrings__DefaultConnection`.
- [ ] `Authentication:Authority` ve `Authentication:Audience` ayarlarını girin; sağlayıcıda parola politikası ve MFA'yı zorunlu kılın.
<!--#endif-->
- [ ] `AllowedHosts` değerini alan adınızla sınırlayın (ör. `api.ornek.com`).
- [ ] `Cors:AllowedOrigins` listesine yalnızca kendi istemci adreslerinizi yazın.
- [ ] Ters vekil sunucu (nginx, IIS ARR, yük dengeleyici) kullanıyorsanız adresini `ForwardedHeaders:KnownProxies` listesine ekleyin.
- [ ] Veritabanı kullanıcısına yalnızca gereken yetkileri verin; migration'ları uygulama kullanıcısıyla değil, dağıtım adımında daha yetkili bir hesapla uygulayın.
- [ ] HTTPS sertifikasını ve HSTS'yi doğrulayın; mümkünse uygulama yalnızca HTTPS dinlesin.
- [ ] Logları merkezi bir yere gönderin ve `Warning` seviyesindeki güvenlik olayları için uyarı kurun.
- [ ] Bağımlılıkları düzenli güncelleyin; `dotnet list package --vulnerable --include-transitive` ile kontrol edin.

## Güvenlik açığı bildirme

Bir güvenlik açığı bulduysanız lütfen herkese açık bir issue açmayın; proje sahibine doğrudan bildirin.
