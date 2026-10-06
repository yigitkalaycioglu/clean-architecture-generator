namespace __Name__.Core.Utilities.Results;

public class ErrorResult(string? message = null) : Result(false, message);
