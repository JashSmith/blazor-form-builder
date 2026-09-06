namespace BlazorFormBuilder.Core.Storage;

public sealed class OptimisticConcurrencyException(string message) : Exception(message);
