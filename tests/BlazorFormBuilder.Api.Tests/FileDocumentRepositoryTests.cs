using BlazorFormBuilder.Api.Persistence;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Services;

namespace BlazorFormBuilder.Api.Tests;

public sealed class FileDocumentRepositoryTests : IDisposable
{
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "blazor-form-builder-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SavedWorkspaceSurvivesRepositoryRestart()
    {
        var workspace = PageBuilderService.CreateWorkspace("Portal");
        using var firstRepository = CreateRepository();

        var saved = await firstRepository.SaveAsync(tenantId, workspace, expectedRevision: null);
        using var restartedRepository = CreateRepository();
        var restored = await restartedRepository.GetAsync(tenantId, workspace.Id);

        Assert.Equal(1, saved.Revision);
        Assert.NotNull(restored);
        Assert.Equal("Portal", restored.Document.Name);
        Assert.Equal(1, restored.Revision);
    }

    [Fact]
    public async Task StaleRevisionCannotOverwriteNewerWorkspace()
    {
        var workspace = PageBuilderService.CreateWorkspace("Portal");
        using var repository = CreateRepository();
        var first = await repository.SaveAsync(tenantId, workspace, expectedRevision: null);
        workspace.Name = "Portal v2";
        var second = await repository.SaveAsync(tenantId, workspace, first.Revision);

        workspace.Name = "Stale edit";
        var exception = await Assert.ThrowsAsync<DocumentConcurrencyException>(async () =>
            await repository.SaveAsync(tenantId, workspace, first.Revision));

        Assert.Equal(2, second.Revision);
        Assert.Equal(2, exception.CurrentRevision);
        Assert.Equal("Portal v2", (await repository.GetAsync(tenantId, workspace.Id))?.Document.Name);
    }

    [Fact]
    public async Task TenantsCannotReadEachOthersDocuments()
    {
        var workspace = PageBuilderService.CreateWorkspace("Tenant A portal");
        using var repository = CreateRepository();
        await repository.SaveAsync(tenantId, workspace, expectedRevision: null);

        var otherTenantDocument = await repository.GetAsync(Guid.NewGuid(), workspace.Id);

        Assert.Null(otherTenantDocument);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private FileDocumentRepository<BuilderWorkspaceDefinition> CreateRepository() =>
        new(directory, workspace => workspace.Id);
}
