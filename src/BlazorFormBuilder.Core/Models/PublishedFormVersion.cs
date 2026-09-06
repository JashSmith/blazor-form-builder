namespace BlazorFormBuilder.Core.Models;

public sealed class PublishedFormVersion
{
    public required Guid PublicationId { get; init; }

    public required Guid FormId { get; init; }

    public required int Version { get; init; }

    public required DateTimeOffset PublishedAtUtc { get; init; }

    public required Guid PublishedByUserId { get; init; }

    public required FormDefinition Definition { get; init; }
}
