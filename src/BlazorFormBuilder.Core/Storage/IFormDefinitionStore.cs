using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Core.Storage;

public interface IFormDefinitionStore
{
    ValueTask<FormDefinition?> LoadAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(FormDefinition definition, CancellationToken cancellationToken = default);
}
