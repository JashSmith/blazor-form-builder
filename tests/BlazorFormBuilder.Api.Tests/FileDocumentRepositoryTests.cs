using BlazorFormBuilder.Api.Persistence;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Services;

namespace BlazorFormBuilder.Api.Tests;

public sealed class FileDocumentRepositoryTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "blazor-form-builder-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SavedWorkspaceSurvivesRepositoryRestart()
    {
        var workspace = PageBuilderService.CreateWorkspace("Portal");
        var firstRepository = CreateRepository();

        var saved = await firstRepository.SaveAsync(workspace, expectedRevision: null);
        var restartedRepository = CreateRepository();
        var restored = await restartedRepository.GetAsync(workspace.Id);

        Assert.Equal(1, saved.Revision);
        Assert.NotNull(restored);
        Assert.Equal("Portal", restored.Document.Name);
        Assert.Equal(1, restored.Revision);
    }

    [Fact]
    public async Task StaleRevisionCannotOverwriteNewerWorkspace()
    {
        var workspace = PageBuilderService.CreateWorkspace("Portal");
        var repository = CreateRepository();
        var first = await repository.SaveAsync(workspace, expectedRevision: null);
        workspace.Name = "Portal v2";
        var second = await repository.SaveAsync(workspace, first.Revision);

        workspace.Name = "Stale edit";
        var exception = await Assert.ThrowsAsync<DocumentConcurrencyException>(async () =>
            await repository.SaveAsync(workspace, first.Revision));

        Assert.Equal(2, second.Revision);
        Assert.Equal(2, exception.CurrentRevision);
        Assert.Equal("Portal v2", (await repository.GetAsync(workspace.Id))?.Document.Name);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private FileDocumentRepository<BuilderWorkspaceDefinition> CreateRepository() =>
        new(directory, workspace => workspace.Id);
}
