namespace BlazorFormBuilder.Core.Models;

public enum ContentDirection
{
    LeftToRight,
    RightToLeft
}

public sealed class LanguageDefinition
{
    public required string Code { get; set; }

    public required string DisplayName { get; set; }

    public ContentDirection Direction { get; set; }
}

public sealed class LocalizationDefinition
{
    public string DefaultLanguageCode { get; set; } = "en";

    public List<LanguageDefinition> Languages { get; init; } =
    [
        new() { Code = "en", DisplayName = "English", Direction = ContentDirection.LeftToRight },
        new() { Code = "fa", DisplayName = "فارسی", Direction = ContentDirection.RightToLeft }
    ];
}

public sealed class LocalizedTextDefinition
{
    public string Key { get; set; } = string.Empty;

    public Dictionary<string, string> Values { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public string Resolve(string? languageCode, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(languageCode) &&
            Values.TryGetValue(languageCode, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return Values.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? fallback;
    }
}
