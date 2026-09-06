using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Storage;

namespace BlazorFormBuilder.App.Storage;

public sealed class ServerBuilderWorkspaceStore(HttpClient httpClient) : IBuilderWorkspaceStore
{
    private EntityTagHeaderValue? entityTag;

    public async ValueTask<BuilderWorkspaceDefinition?> LoadAsync(CancellationToken cancellationToken = default)
    {
        entityTag = null;
        using var response = await httpClient.GetAsync("api/workspaces/current", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        entityTag = response.Headers.ETag;
        return await response.Content.ReadFromJsonAsync<BuilderWorkspaceDefinition>(cancellationToken);
    }

    public async ValueTask SaveAsync(BuilderWorkspaceDefinition workspace, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/workspaces/{workspace.Id}")
        {
            Content = JsonContent.Create(workspace)
        };
        if (entityTag is not null)
        {
            request.Headers.IfMatch.Add(entityTag);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new OptimisticConcurrencyException("Workspace changed on the server. Reload before saving again.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new AuthorizationDeniedException("Viewer access is read-only. Ask an owner to change your role.");
        }

        response.EnsureSuccessStatusCode();
        entityTag = response.Headers.ETag;
    }
}
