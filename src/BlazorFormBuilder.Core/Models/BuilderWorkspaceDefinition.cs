namespace BlazorFormBuilder.Core.Models;

public sealed class BuilderWorkspaceDefinition
{
    public int SchemaVersion { get; init; } = 1;

    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public Guid? ActivePageId { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<PageDefinition> Pages { get; init; } = [];
}
