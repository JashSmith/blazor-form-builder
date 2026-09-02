using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Core.Services;

public static class FormDefinitionService
{
    public static FormDefinition Create(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = NormalizeName(name)
    };

    public static void AddField(FormDefinition form, FormFieldDefinition field)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(field);

        if (form.Fields.Any(item => item.Id == field.Id))
        {
            throw new InvalidOperationException($"A field with id '{field.Id}' already exists.");
        }

        if (form.Fields.Any(item => string.Equals(item.Key, field.Key, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A field with key '{field.Key}' already exists.");
        }

        field.Order = form.Fields.Count;
        form.Fields.Add(field);
    }

    public static void RemoveField(FormDefinition form, Guid fieldId)
    {
        ArgumentNullException.ThrowIfNull(form);
        form.Fields.RemoveAll(field => field.Id == fieldId);
        NormalizeOrder(form);
    }

    public static bool MoveField(FormDefinition form, Guid fieldId, int offset)
    {
        ArgumentNullException.ThrowIfNull(form);

        var currentIndex = form.Fields.FindIndex(field => field.Id == fieldId);
        var targetIndex = currentIndex + offset;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= form.Fields.Count)
        {
            return false;
        }

        (form.Fields[currentIndex], form.Fields[targetIndex]) =
            (form.Fields[targetIndex], form.Fields[currentIndex]);
        NormalizeOrder(form);
        return true;
    }

    public static string CreateUniqueKey(FormDefinition form, string type)
    {
        ArgumentNullException.ThrowIfNull(form);

        var baseKey = new string(type.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        baseKey = string.IsNullOrWhiteSpace(baseKey) ? "field" : baseKey;
        var suffix = 1;
        var candidate = baseKey;

        while (form.Fields.Any(field => string.Equals(field.Key, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{baseKey}{++suffix}";
        }

        return candidate;
    }

    private static string NormalizeName(string name) =>
        string.IsNullOrWhiteSpace(name) ? "Untitled form" : name.Trim();

    private static void NormalizeOrder(FormDefinition form)
    {
        for (var index = 0; index < form.Fields.Count; index++)
        {
            form.Fields[index].Order = index;
        }
    }
}
