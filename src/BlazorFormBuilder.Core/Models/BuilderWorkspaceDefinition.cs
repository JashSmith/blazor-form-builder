namespace BlazorFormBuilder.Core.Models;

public sealed class BuilderWorkspaceDefinition
{
    public int SchemaVersion { get; init; } = 2;

    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public Guid? ActivePageId { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public LocalizationDefinition Localization { get; init; } = new();

    public List<HeaderDefinition> Headers { get; init; } = [];

    public List<FooterDefinition> Footers { get; init; } = [];

    public List<PageDefinition> Pages { get; init; } = [];
}
