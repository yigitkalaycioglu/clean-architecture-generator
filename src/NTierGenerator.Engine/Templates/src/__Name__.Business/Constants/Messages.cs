namespace __Name__.Business.Constants;

/// <summary>
/// İş katmanının kullanıcıya döndürdüğü mesajlar. Metinler tek yerde durduğu için
/// değiştirmek ya da çoklu dil desteği eklemek kolaylaşır.
/// </summary>
public static class Messages
{
    // Örnek: public const string OrderCreated = "Sipariş oluşturuldu.";
//#if Sample

    public const string CategoryAdded = "Kategori eklendi.";
    public const string CategoryUpdated = "Kategori güncellendi.";
    public const string CategoryDeleted = "Kategori silindi.";
    public const string CategoryNotFound = "Kategori bulunamadı.";
    public const string CategoryNameAlreadyExists = "Bu adla bir kategori zaten var.";
    public const string CategoryHasProducts = "Ürünü olan bir kategori silinemez.";

    public const string ProductsListed = "Ürünler listelendi.";
    public const string ProductAdded = "Ürün eklendi.";
    public const string ProductUpdated = "Ürün güncellendi.";
    public const string ProductDeleted = "Ürün silindi.";
    public const string ProductNotFound = "Ürün bulunamadı.";
    public const string ProductNameAlreadyExists = "Bu adla bir ürün zaten var.";

    public static string CategoryProductLimitExceeded(int limit) => $"Bir kategoride en fazla {limit} ürün olabilir.";
//#endif
//#if Auth

    public const string AuthenticationRequired = "Bu işlem için giriş yapmalısınız.";
    public const string AuthorizationDenied = "Bu işlem için yetkiniz yok.";
    public const string UserRegistered = "Kayıt başarılı.";
    public const string UserAlreadyExists = "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.";
    public const string UserNotFound = "Kullanıcı bulunamadı.";
    public const string UserInactive = "Kullanıcı hesabı pasif durumda.";
    public const string InvalidCredentials = "E-posta adresi ya da parola hatalı.";
    public const string SuccessfulLogin = "Giriş başarılı.";
//#endif
}
