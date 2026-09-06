using BlazorFormBuilder.Api.Persistence;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Services;

namespace BlazorFormBuilder.Api.Tests;

public sealed class FilePublishedFormRepositoryTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "blazor-form-builder-publication-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task PublishingCreatesNumberedDetachedSnapshots()
    {
        using var repository = new FilePublishedFormRepository(directory);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var form = FormDefinitionService.Create("Request");
        form.Fields.Add(new()
        {
            Id = Guid.NewGuid(),
            Type = "text",
            Key = "name",
            Label = "Name"
        });

        var first = await repository.PublishAsync(tenantId, userId, form);
        form.Name = "Changed draft";
        var second = await repository.PublishAsync(tenantId, userId, form);
        var restoredFirst = await repository.GetAsync(tenantId, form.Id, 1);

        Assert.Equal(1, first.Version);
        Assert.Equal(2, second.Version);
        Assert.Equal("Request", restoredFirst!.Definition.Name);
        Assert.Equal("Changed draft", second.Definition.Name);
    }

    [Fact]
    public async Task PublicationsAreTenantIsolated()
    {
        using var repository = new FilePublishedFormRepository(directory);
        var tenantId = Guid.NewGuid();
        var form = FormDefinitionService.Create("Private");
        await repository.PublishAsync(tenantId, Guid.NewGuid(), form);

        Assert.Single(await repository.ListAsync(tenantId));
        Assert.Empty(await repository.ListAsync(Guid.NewGuid()));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
        GC.SuppressFinalize(this);
    }
}
