using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Storage;

namespace BlazorFormBuilder.App.Storage;

public sealed class ResilientBuilderWorkspaceStore(
    ServerBuilderWorkspaceStore server,
    BrowserBuilderWorkspaceStore browser) : IBuilderWorkspaceStore
{
    public async ValueTask<BuilderWorkspaceDefinition?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var workspace = await server.LoadAsync(cancellationToken);
            if (workspace is not null)
            {
                await browser.SaveAsync(workspace, cancellationToken);
                return workspace;
            }
        }
        catch (HttpRequestException)
        {
            // Standalone/offline mode uses the last local snapshot.
        }

        return await browser.LoadAsync(cancellationToken);
    }

    public async ValueTask SaveAsync(BuilderWorkspaceDefinition workspace, CancellationToken cancellationToken = default)
    {
        try
        {
            await server.SaveAsync(workspace, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Keep the editor usable when the API is offline.
        }

        await browser.SaveAsync(workspace, cancellationToken);
    }
}
