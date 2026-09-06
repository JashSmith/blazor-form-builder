using System.Text.Json;
using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Api.Persistence;

public sealed class FilePublishedFormRepository(string directory) : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim gate = new(1, 1);

    public async ValueTask<PublishedFormVersion> PublishAsync(
        Guid tenantId,
        Guid userId,
        FormDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var formDirectory = FormDirectory(tenantId, definition.Id);
            Directory.CreateDirectory(formDirectory);
            var versions = Directory.EnumerateFiles(formDirectory, "*.json", SearchOption.TopDirectoryOnly)
                .Select(path => int.TryParse(Path.GetFileNameWithoutExtension(path), out var version) ? version : 0);
            var nextVersion = versions.DefaultIfEmpty().Max() + 1;
            var snapshot = JsonSerializer.Deserialize<FormDefinition>(
                JsonSerializer.Serialize(definition, SerializerOptions),
                SerializerOptions)!;
            var publication = new PublishedFormVersion
            {
                PublicationId = Guid.NewGuid(),
                FormId = definition.Id,
                Version = nextVersion,
                PublishedAtUtc = DateTimeOffset.UtcNow,
                PublishedByUserId = userId,
                Definition = snapshot
            };
            var path = VersionPath(tenantId, definition.Id, nextVersion);
            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, publication, SerializerOptions, cancellationToken);
            return publication;
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<IReadOnlyList<PublishedFormVersion>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var tenantDirectory = TenantDirectory(tenantId);
            if (!Directory.Exists(tenantDirectory))
            {
                return [];
            }

            var publications = new List<PublishedFormVersion>();
            foreach (var path in Directory.EnumerateFiles(tenantDirectory, "*.json", SearchOption.AllDirectories))
            {
                await using var stream = File.OpenRead(path);
                var publication = await JsonSerializer.DeserializeAsync<PublishedFormVersion>(
                    stream,
                    SerializerOptions,
                    cancellationToken);
                if (publication is not null)
                {
                    publications.Add(publication);
                }
            }
            return publications
                .OrderByDescending(publication => publication.PublishedAtUtc)
                .ToArray();
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<PublishedFormVersion?> GetAsync(
        Guid tenantId,
        Guid formId,
        int version,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var path = VersionPath(tenantId, formId, version);
            if (!File.Exists(path))
            {
                return null;
            }
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<PublishedFormVersion>(
                stream,
                SerializerOptions,
                cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private string TenantDirectory(Guid tenantId) => Path.Combine(directory, tenantId.ToString("N"));

    private string FormDirectory(Guid tenantId, Guid formId) =>
        Path.Combine(TenantDirectory(tenantId), formId.ToString("N"));

    private string VersionPath(Guid tenantId, Guid formId, int version) =>
        Path.Combine(FormDirectory(tenantId, formId), $"{version}.json");

    public void Dispose()
    {
        gate.Dispose();
        GC.SuppressFinalize(this);
    }
}
