using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Storage;

namespace BlazorFormBuilder.App.Storage;

public sealed class ServerFormDefinitionStore(HttpClient httpClient) : IFormDefinitionStore
{
    private EntityTagHeaderValue? entityTag;

    public async ValueTask<FormDefinition?> LoadAsync(CancellationToken cancellationToken = default)
    {
        entityTag = null;
        using var response = await httpClient.GetAsync("api/forms/current", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        entityTag = response.Headers.ETag;
        return await response.Content.ReadFromJsonAsync<FormDefinition>(cancellationToken);
    }

    public async ValueTask SaveAsync(FormDefinition definition, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/forms/{definition.Id}")
        {
            Content = JsonContent.Create(definition)
        };
        if (entityTag is not null)
        {
            request.Headers.IfMatch.Add(entityTag);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new OptimisticConcurrencyException("Form changed on the server. Reload before saving again.");
        }

        response.EnsureSuccessStatusCode();
        entityTag = response.Headers.ETag;
    }
}
