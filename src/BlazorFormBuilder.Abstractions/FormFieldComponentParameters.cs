using BlazorFormBuilder.Core.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorFormBuilder.Abstractions;

public static class FormFieldComponentParameters
{
    public const string Field = nameof(Field);
    public const string Value = nameof(Value);
    public const string ValueChanged = nameof(ValueChanged);

    public static IDictionary<string, object> ForDesigner(FormFieldDefinition fieldDefinition) =>
        new Dictionary<string, object> { [Field] = fieldDefinition };

    public static IDictionary<string, object> ForRuntime(
        FormFieldDefinition fieldDefinition,
        string? value,
        EventCallback<string?> valueChanged) => new Dictionary<string, object>
        {
            [Field] = fieldDefinition,
            [Value] = value ?? string.Empty,
            [ValueChanged] = valueChanged
        };
}
