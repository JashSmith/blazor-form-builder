using System.Text.Json;
using BlazorFormBuilder.App.Auth;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Storage;
using Microsoft.JSInterop;

namespace BlazorFormBuilder.App.Storage;

public sealed class BrowserBuilderWorkspaceStore(IJSRuntime javaScript, TenantSessionState tenantSession) : IBuilderWorkspaceStore
{
    private const string BaseStorageKey = "blazor-form-builder:workspace";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<BuilderWorkspaceDefinition?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var json = await javaScript.InvokeAsync<string?>(
            "blazorFormBuilderStorage.get",
            cancellationToken,
            tenantSession.StorageKey(BaseStorageKey));

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<BuilderWorkspaceDefinition>(json, SerializerOptions);
    }

    public async ValueTask SaveAsync(
        BuilderWorkspaceDefinition workspace,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var json = JsonSerializer.Serialize(workspace, SerializerOptions);

        await javaScript.InvokeVoidAsync(
            "blazorFormBuilderStorage.set",
            cancellationToken,
            tenantSession.StorageKey(BaseStorageKey),
            json);
    }
}
