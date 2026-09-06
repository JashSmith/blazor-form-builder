using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Core.Storage;

public interface IWorkflowDefinitionStore
{
    ValueTask<WorkflowDefinition?> LoadAsync(CancellationToken cancellationToken = default);

    ValueTask SaveAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);
}
