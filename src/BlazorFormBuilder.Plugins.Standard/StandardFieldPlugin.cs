using System.Globalization;
using System.Net.Mail;
using BlazorFormBuilder.Abstractions;
using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Plugins.Standard;

public sealed class StandardFieldPlugin(
    string type,
    string displayName,
    string defaultLabel,
    string? defaultPlaceholder = null) : IFormFieldPlugin
{
    public string Type { get; } = type;

    public string DisplayName { get; } = displayName;

    public System.Type DesignerComponentType => typeof(StandardFieldDesigner);

    public System.Type RuntimeComponentType => typeof(StandardFieldRuntime);

    public FormFieldDefinition CreateField(string key) => new()
    {
        Id = Guid.NewGuid(),
        Type = Type,
        Key = key,
        Label = defaultLabel,
        Placeholder = defaultPlaceholder
    };

    public IReadOnlyList<string> Validate(FormFieldDefinition fieldDefinition, string? value)
    {
        ArgumentNullException.ThrowIfNull(fieldDefinition);

        if (fieldDefinition.IsRequired && IsMissing(value))
        {
            return [$"{fieldDefinition.Label} is required."];
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return Type switch
        {
            "email" when !MailAddress.TryCreate(value, out _) => [$"{fieldDefinition.Label} must be a valid email address."],
            "number" when !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _) => [$"{fieldDefinition.Label} must be a number."],
            "date" when !DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) => [$"{fieldDefinition.Label} must be a valid date."],
            _ => []
        };
    }

    private bool IsMissing(string? value) => string.IsNullOrWhiteSpace(value) ||
        (Type == "checkbox" && !string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase));
}
