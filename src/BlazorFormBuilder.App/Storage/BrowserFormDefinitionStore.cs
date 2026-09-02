using System.Text.Json;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Storage;
using Microsoft.JSInterop;

namespace BlazorFormBuilder.App.Storage;

public sealed class BrowserFormDefinitionStore(IJSRuntime javaScript) : IFormDefinitionStore
{
    private const string StorageKey = "blazor-form-builder:draft";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<FormDefinition?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var json = await javaScript.InvokeAsync<string?>(
            "localStorage.getItem",
            cancellationToken,
            StorageKey);

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<FormDefinition>(json, SerializerOptions);
    }

    public async ValueTask SaveAsync(
        FormDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var json = JsonSerializer.Serialize(definition, SerializerOptions);

        await javaScript.InvokeVoidAsync(
            "localStorage.setItem",
            cancellationToken,
            StorageKey,
            json);
    }
}
