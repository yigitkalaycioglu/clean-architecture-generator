namespace __Name__.Core.Utilities.Results;

public class ErrorDataResult<T> : DataResult<T>
{
    public ErrorDataResult(string? message = null)
        : base(default, false, message)
    {
    }

    public ErrorDataResult(T data, string? message = null)
        : base(data, false, message)
    {
    }
}
