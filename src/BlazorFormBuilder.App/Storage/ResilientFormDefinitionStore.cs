using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Storage;

namespace BlazorFormBuilder.App.Storage;

public sealed class ResilientFormDefinitionStore(
    ServerFormDefinitionStore server,
    BrowserFormDefinitionStore browser) : IFormDefinitionStore
{
    public async ValueTask<FormDefinition?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var form = await server.LoadAsync(cancellationToken);
            if (form is not null)
            {
                await browser.SaveAsync(form, cancellationToken);
                return form;
            }
        }
        catch (HttpRequestException)
        {
            // Standalone/offline mode uses the last local snapshot.
        }

        return await browser.LoadAsync(cancellationToken);
    }

    public async ValueTask SaveAsync(FormDefinition definition, CancellationToken cancellationToken = default)
    {
        try
        {
            await server.SaveAsync(definition, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Keep the editor usable when the API is offline.
        }

        await browser.SaveAsync(definition, cancellationToken);
    }
}
