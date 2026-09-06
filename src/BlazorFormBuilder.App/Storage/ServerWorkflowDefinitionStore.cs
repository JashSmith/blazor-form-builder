using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Storage;

namespace BlazorFormBuilder.App.Storage;

public sealed class ServerWorkflowDefinitionStore(HttpClient httpClient) : IWorkflowDefinitionStore
{
    private EntityTagHeaderValue? entityTag;

    public async ValueTask<WorkflowDefinition?> LoadAsync(CancellationToken cancellationToken = default)
    {
        entityTag = null;
        using var response = await httpClient.GetAsync("api/workflows/current", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        entityTag = response.Headers.ETag;
        return await response.Content.ReadFromJsonAsync<WorkflowDefinition>(cancellationToken);
    }

    public async ValueTask SaveAsync(
        WorkflowDefinition workflow,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/workflows/{workflow.Id}")
        {
            Content = JsonContent.Create(workflow)
        };
        if (entityTag is not null)
        {
            request.Headers.IfMatch.Add(entityTag);
        }
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new OptimisticConcurrencyException("Workflow changed on the server. Reload before saving again.");
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new AuthorizationDeniedException("Viewer access is read-only. Ask an owner to change your role.");
        }
        response.EnsureSuccessStatusCode();
        entityTag = response.Headers.ETag;
    }
}
