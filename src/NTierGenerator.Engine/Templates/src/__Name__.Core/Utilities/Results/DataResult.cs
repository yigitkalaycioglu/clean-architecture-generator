namespace __Name__.Core.Utilities.Results;

public class DataResult<T>(T? data, bool success, string? message = null) : Result(success, message), IDataResult<T>
{
    public T? Data { get; } = data;
}
