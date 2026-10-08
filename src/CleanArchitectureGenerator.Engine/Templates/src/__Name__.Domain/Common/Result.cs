namespace __Name__.Domain.Common;

/// <summary>
/// Genel kodun (ör. doğrulama adımı) yanıt türünü bilmeden başarısız bir sonuç üretebilmesini sağlar.
/// </summary>
public interface IFailureFactory<TSelf>
    where TSelf : IFailureFactory<TSelf>
{
    static abstract TSelf CreateFailure(Error error);
}

/// <summary>Bir işlemin sonucu: başarılı ya da tek bir <see cref="Common.Error"/> ile başarısız.</summary>
public class Result : IFailureFactory<Result>
{
    protected Result(bool isSuccess, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (isSuccess == (error != Error.None))
        {
            throw new ArgumentException("Başarılı sonuç hata içeremez, başarısız sonuç bir hata içermelidir.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    public static implicit operator Result(Error error) => Failure(error);

    static Result IFailureFactory<Result>.CreateFailure(Error error) => Failure(error);
}

/// <summary>Başarılıysa bir değer taşıyan sonuç.</summary>
public sealed class Result<TValue> : Result, IFailureFactory<Result<TValue>>
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>Başarılı sonucun değeri. Başarısız bir sonuçta okunursa istisna fırlatır.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Başarısız bir sonucun değeri okunamaz.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    static Result<TValue> IFailureFactory<Result<TValue>>.CreateFailure(Error error) => Failure<TValue>(error);
}
