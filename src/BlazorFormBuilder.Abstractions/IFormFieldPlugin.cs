using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Abstractions;

public interface IFormFieldPlugin
{
    string Type { get; }

    string DisplayName { get; }

    System.Type PreviewComponentType { get; }

    FormFieldDefinition CreateField(string key);
}
