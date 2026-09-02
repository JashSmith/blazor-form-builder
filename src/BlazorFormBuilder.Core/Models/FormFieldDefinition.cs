namespace BlazorFormBuilder.Core.Models;

public sealed class FormFieldDefinition
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required string Key { get; set; }

    public required string Label { get; set; }

    public string? Placeholder { get; set; }

    public bool IsRequired { get; set; }

    public int Order { get; set; }
}
