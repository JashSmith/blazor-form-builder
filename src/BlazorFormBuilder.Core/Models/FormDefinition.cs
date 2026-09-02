namespace BlazorFormBuilder.Core.Models;

public sealed class FormDefinition
{
    public int SchemaVersion { get; init; } = 2;

    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public LocalizedTextDefinition Title { get; init; } = new() { Key = "form.title" };

    public LocalizationDefinition Localization { get; init; } = new();

    public List<string> LanguageCodes { get; init; } = ["en"];

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<FormFieldDefinition> Fields { get; init; } = [];
}
