using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Abstractions;

public interface IFormFieldPlugin
{
    string Type { get; }

    string DisplayName { get; }

    System.Type DesignerComponentType { get; }

    System.Type RuntimeComponentType { get; }

    FormFieldDefinition CreateField(string key);

    IReadOnlyList<string> Validate(FormFieldDefinition fieldDefinition, string? value);
}
