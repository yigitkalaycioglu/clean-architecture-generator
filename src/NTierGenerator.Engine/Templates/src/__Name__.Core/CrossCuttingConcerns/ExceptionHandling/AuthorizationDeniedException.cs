namespace __Name__.Core.CrossCuttingConcerns.ExceptionHandling;

/// <summary>
/// Kullanıcı giriş yapmamışsa ya da gerekli yetkiye sahip değilse fırlatılır.
/// <see cref="GlobalExceptionHandler"/> bunu 401 ya da 403 olarak döndürür.
/// </summary>
public sealed class AuthorizationDeniedException(string message) : Exception(message);
