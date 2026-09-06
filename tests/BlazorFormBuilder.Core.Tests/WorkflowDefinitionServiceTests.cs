using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Services;

namespace BlazorFormBuilder.Core.Tests;

public sealed class WorkflowDefinitionServiceTests
{
    [Fact]
    public void UserTaskRequiresAnExactPublishedFormVersion()
    {
        var workflow = WorkflowDefinitionService.Create("Approval");
        var task = WorkflowDefinitionService.AddUserTask(workflow, "Review submission");
        var form = new FormDefinition { Id = Guid.NewGuid(), Name = "Request" };
        var publication = new PublishedFormVersion
        {
            PublicationId = Guid.NewGuid(),
            FormId = form.Id,
            Version = 3,
            PublishedAtUtc = DateTimeOffset.UtcNow,
            PublishedByUserId = Guid.NewGuid(),
            Definition = form
        };

        Assert.Single(WorkflowDefinitionService.Validate(workflow));

        WorkflowDefinitionService.AssignForm(task, publication);

        Assert.Empty(WorkflowDefinitionService.Validate(workflow));
        Assert.Equal(publication.FormId, task.FormId);
        Assert.Equal(3, task.FormVersion);
    }

    [Fact]
    public void RemovingTaskNormalizesRemainingOrder()
    {
        var workflow = WorkflowDefinitionService.Create("Approval");
        var first = WorkflowDefinitionService.AddUserTask(workflow, "First");
        WorkflowDefinitionService.AddUserTask(workflow, "Second");

        WorkflowDefinitionService.RemoveUserTask(workflow, first.Id);

        Assert.Single(workflow.UserTasks);
        Assert.Equal(0, workflow.UserTasks[0].Order);
    }
}
