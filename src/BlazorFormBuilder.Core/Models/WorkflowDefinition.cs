namespace BlazorFormBuilder.Core.Models;

public sealed class WorkflowDefinition
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<WorkflowUserTaskDefinition> UserTasks { get; init; } = [];
}

public sealed class WorkflowUserTaskDefinition
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public int Order { get; set; }

    public Guid? FormId { get; set; }

    public int? FormVersion { get; set; }
}
