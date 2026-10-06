namespace __Name__.Core.Utilities.Results;

/// <summary>
/// İş metotlarının standart dönüş tipi: işlemin başarılı olup olmadığını ve kullanıcıya gösterilecek mesajı taşır.
/// </summary>
public interface IResult
{
    bool Success { get; }

    string? Message { get; }
}
