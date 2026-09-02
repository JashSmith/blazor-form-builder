namespace BlazorFormBuilder.Core.Models;

public sealed class FormDefinition
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public List<FormFieldDefinition> Fields { get; init; } = [];
}
