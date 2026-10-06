namespace __Name__.Core.Utilities.Results;

public class SuccessDataResult<T>(T data, string? message = null) : DataResult<T>(data, true, message);
