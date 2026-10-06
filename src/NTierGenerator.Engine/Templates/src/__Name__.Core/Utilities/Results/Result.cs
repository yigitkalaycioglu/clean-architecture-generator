namespace __Name__.Core.Utilities.Results;

public class Result(bool success, string? message = null) : IResult
{
    public bool Success { get; } = success;

    public string? Message { get; } = message;
}
