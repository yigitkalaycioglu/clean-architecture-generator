namespace __Name__.Core.Utilities.Results;

/// <summary>
/// Veri de döndüren iş metotlarının sonucu. Başarısız sonuçlarda <see cref="Data"/> boş olabilir.
/// </summary>
public interface IDataResult<out T> : IResult
{
    T? Data { get; }
}
