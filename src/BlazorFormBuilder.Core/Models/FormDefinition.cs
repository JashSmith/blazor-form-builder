namespace BlazorFormBuilder.Core.Models;

public sealed class FormDefinition
{
    public int SchemaVersion { get; init; } = 1;

    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<FormFieldDefinition> Fields { get; init; } = [];
}
