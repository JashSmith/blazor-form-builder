using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Core.Storage;

public interface IBuilderWorkspaceStore
{
    ValueTask<BuilderWorkspaceDefinition?> LoadAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(BuilderWorkspaceDefinition workspace, CancellationToken cancellationToken = default);
}
