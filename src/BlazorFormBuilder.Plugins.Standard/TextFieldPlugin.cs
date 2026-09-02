using BlazorFormBuilder.Abstractions;
using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Plugins.Standard;

public sealed class TextFieldPlugin : IFormFieldPlugin
{
    public string Type => "text";

    public string DisplayName => "Text input";

    public Type PreviewComponentType => typeof(TextFieldPreview);

    public FormFieldDefinition CreateField(string key) => new()
    {
        Id = Guid.NewGuid(),
        Type = Type,
        Key = key,
        Label = "Text field",
        Placeholder = "Enter a value"
    };
}
