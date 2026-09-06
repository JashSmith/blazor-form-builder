using System.Text.Json;
using BlazorFormBuilder.App.Auth;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Storage;
using Microsoft.JSInterop;

namespace BlazorFormBuilder.App.Storage;

public sealed class BrowserFormDefinitionStore(IJSRuntime javaScript, TenantSessionState tenantSession) : IFormDefinitionStore
{
    private const string BaseStorageKey = "blazor-form-builder:draft";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<FormDefinition?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var json = await javaScript.InvokeAsync<string?>(
            "blazorFormBuilderStorage.get",
            cancellationToken,
            tenantSession.StorageKey(BaseStorageKey));

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
            "blazorFormBuilderStorage.set",
            cancellationToken,
            tenantSession.StorageKey(BaseStorageKey),
            json);
    }
}
