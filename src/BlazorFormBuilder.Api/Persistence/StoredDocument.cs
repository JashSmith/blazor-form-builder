namespace BlazorFormBuilder.Api.Persistence;

public sealed record StoredDocument<TDocument>(
    TDocument Document,
    long Revision,
    DateTimeOffset SavedAtUtc);

public sealed class DocumentConcurrencyException(long currentRevision) : Exception
{
    public long CurrentRevision { get; } = currentRevision;
}
