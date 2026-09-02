using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Abstractions;

public static class FormFieldPreviewParameters
{
    public const string Field = nameof(Field);

    public static IDictionary<string, object> For(FormFieldDefinition field) =>
        new Dictionary<string, object> { [Field] = field };
}
