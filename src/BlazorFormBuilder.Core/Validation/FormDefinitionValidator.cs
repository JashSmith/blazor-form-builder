using System.Text.RegularExpressions;
using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Core.Validation;

public static partial class FormDefinitionValidator
{
    public static IReadOnlyList<FormValidationIssue> Validate(FormDefinition form)
    {
        ArgumentNullException.ThrowIfNull(form);
        var issues = new List<FormValidationIssue>();

        if (string.IsNullOrWhiteSpace(form.Name))
        {
            issues.Add(new(null, "form.name.required", "Form name is required."));
        }

        if (form.Fields.Count == 0)
        {
            issues.Add(new(null, "form.fields.required", "Add at least one field before saving."));
        }

        foreach (var item in form.Fields)
        {
            if (string.IsNullOrWhiteSpace(item.Label))
            {
                issues.Add(new(item.Id, "field.label.required", "Field label is required."));
            }

            if (string.IsNullOrWhiteSpace(item.Key))
            {
                issues.Add(new(item.Id, "field.key.required", "Field key is required."));
            }
            else if (!FieldKeyPattern().IsMatch(item.Key))
            {
                issues.Add(new(item.Id, "field.key.invalid", "Key must start with a letter and contain only letters, numbers, or underscores."));
            }
        }

        var duplicateKeys = form.Fields
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group);

        issues.AddRange(duplicateKeys.Select(item =>
            new FormValidationIssue(item.Id, "field.key.duplicate", $"The key '{item.Key}' is used more than once.")));

        return issues;
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex FieldKeyPattern();
}
