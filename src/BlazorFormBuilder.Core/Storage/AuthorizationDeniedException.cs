namespace BlazorFormBuilder.Core.Storage;

public sealed class AuthorizationDeniedException(string message) : Exception(message);
