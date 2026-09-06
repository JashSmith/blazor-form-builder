using System.Text.Json;

namespace BlazorFormBuilder.Api.Persistence;

public sealed class FileDocumentRepository<TDocument>(string directory, Func<TDocument, Guid> getId) : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim gate = new(1, 1);

    public async ValueTask<StoredDocument<TDocument>?> GetAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadAsync(PathFor(tenantId, id), cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<StoredDocument<TDocument>?> GetLatestAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var tenantDirectory = TenantDirectory(tenantId);
            if (!Directory.Exists(tenantDirectory))
            {
                return null;
            }

            StoredDocument<TDocument>? latest = null;
            foreach (var path in Directory.EnumerateFiles(tenantDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var candidate = await ReadAsync(path, cancellationToken);
                if (candidate is not null && (latest is null || candidate.SavedAtUtc > latest.SavedAtUtc))
                {
                    latest = candidate;
                }
            }

            return latest;
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<StoredDocument<TDocument>> SaveAsync(
        Guid tenantId,
        TDocument document,
        long? expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        await gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(TenantDirectory(tenantId));
            var id = getId(document);
            var path = PathFor(tenantId, id);
            var current = await ReadAsync(path, cancellationToken);

            if (current is not null && expectedRevision != current.Revision)
            {
                throw new DocumentConcurrencyException(current.Revision);
            }

            if (current is null && expectedRevision is not null and not 0)
            {
                throw new DocumentConcurrencyException(0);
            }

            var stored = new StoredDocument<TDocument>(document, (current?.Revision ?? 0) + 1, DateTimeOffset.UtcNow);
            var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, stored, SerializerOptions, cancellationToken);
            }

            File.Move(temporaryPath, path, true);
            return stored;
        }
        finally
        {
            gate.Release();
        }
    }

    private string TenantDirectory(Guid tenantId) => Path.Combine(directory, tenantId.ToString("N"));

    private string PathFor(Guid tenantId, Guid id) => Path.Combine(TenantDirectory(tenantId), $"{id:N}.json");

    private static async ValueTask<StoredDocument<TDocument>?> ReadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<StoredDocument<TDocument>>(stream, SerializerOptions, cancellationToken);
    }

    public void Dispose()
    {
        gate.Dispose();
        GC.SuppressFinalize(this);
    }
}
