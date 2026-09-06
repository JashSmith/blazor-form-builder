using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Core.Services;

public static class WorkflowDefinitionService
{
    public static WorkflowDefinition Create(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = NormalizeName(name)
    };

    public static WorkflowUserTaskDefinition AddUserTask(WorkflowDefinition workflow, string name)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var task = new WorkflowUserTaskDefinition
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(name),
            Order = workflow.UserTasks.Count
        };
        workflow.UserTasks.Add(task);
        Touch(workflow);
        return task;
    }

    public static void RemoveUserTask(WorkflowDefinition workflow, Guid taskId)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        workflow.UserTasks.RemoveAll(task => task.Id == taskId);
        for (var index = 0; index < workflow.UserTasks.Count; index++)
        {
            workflow.UserTasks[index].Order = index;
        }
        Touch(workflow);
    }

    public static void AssignForm(
        WorkflowUserTaskDefinition task,
        PublishedFormVersion? publication)
    {
        ArgumentNullException.ThrowIfNull(task);
        task.FormId = publication?.FormId;
        task.FormVersion = publication?.Version;
    }

    public static IReadOnlyList<string> Validate(WorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(workflow.Name))
        {
            issues.Add("Workflow name is required.");
        }
        foreach (var task in workflow.UserTasks)
        {
            if (string.IsNullOrWhiteSpace(task.Name))
            {
                issues.Add("Every user task needs a name.");
            }
            if (task.FormId is null || task.FormVersion is null)
            {
                issues.Add($"User task '{task.Name}' needs a published form version.");
            }
        }
        return issues;
    }

    public static void Touch(WorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        workflow.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizeName(string name) =>
        string.IsNullOrWhiteSpace(name) ? "Untitled workflow" : name.Trim();
}
