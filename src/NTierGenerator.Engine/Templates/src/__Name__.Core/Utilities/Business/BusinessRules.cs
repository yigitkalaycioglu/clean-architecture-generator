using __Name__.Core.Utilities.Results;

namespace __Name__.Core.Utilities.Business;

/// <summary>
/// İş kurallarını tek satırda çalıştırmak için yardımcı. Kurallar sırayla değerlendirilir
/// ve ilk başarısız sonuç döndürülür; hepsi başarılıysa <c>null</c> döner.
/// </summary>
public static class BusinessRules
{
    public static IResult? Run(params IResult[] logics)
    {
        return logics.FirstOrDefault(logic => !logic.Success);
    }

    /// <summary>
    /// Asenkron kurallar. İlk başarısız kuralda durulur, sonraki kurallar hiç çalıştırılmaz
    /// (gereksiz veritabanı sorgusu yapılmaz).
    /// </summary>
    public static async Task<IResult?> RunAsync(params Func<Task<IResult>>[] logics)
    {
        foreach (var logic in logics)
        {
            var result = await logic();
            if (!result.Success)
            {
                return result;
            }
        }

        return null;
    }
}
