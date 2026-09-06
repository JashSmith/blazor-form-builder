using System.Net;
using System.Net.Http.Json;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Storage;

namespace BlazorFormBuilder.App.Storage;

public sealed class ServerPublishedFormStore(HttpClient httpClient) : IPublishedFormStore
{
    public async ValueTask<IReadOnlyList<PublishedFormVersion>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<PublishedFormVersion[]>("api/publications", cancellationToken) ?? [];

    public async ValueTask<PublishedFormVersion> PublishAsync(
        Guid formId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync(
            $"api/publications/forms/{formId}",
            content: null,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new AuthorizationDeniedException("Viewer access is read-only. Ask an owner to publish this form.");
        }
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublishedFormVersion>(cancellationToken))!;
    }
}
