using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace BlazorFormBuilder.Api.Auth;

public sealed class FileTenantUserRepository(string path) : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly PasswordHasher<TenantUserRecord> passwordHasher = new();

    public async ValueTask<TenantUserRecord?> RegisterTenantAsync(
        string tenantName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var tenantSlug = CreateSlug(tenantName);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            if (users.Any(user =>
                string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.TenantSlug, tenantSlug, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var user = new TenantUserRecord
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                TenantName = tenantName.Trim(),
                TenantSlug = tenantSlug,
                Email = normalizedEmail,
                PasswordHash = string.Empty
            };
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            users.Add(user);
            await SaveAsync(users, cancellationToken);
            return user;
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<TenantUserRecord?> ValidateCredentialsAsync(
        string tenantSlug,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var user = users.FirstOrDefault(candidate =>
                string.Equals(candidate.TenantSlug, tenantSlug.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(candidate.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                return null;
            }

            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return result == PasswordVerificationResult.Failed ? null : user;
        }
        finally
        {
            gate.Release();
        }
    }

    public static string CreateSlug(string tenantName)
    {
        var characters = tenantName.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        var slug = string.Join('-', new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? "workspace" : slug;
    }

    private async ValueTask<List<TenantUserRecord>> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<TenantUserRecord>>(stream, SerializerOptions, cancellationToken) ?? [];
    }

    private async ValueTask SaveAsync(List<TenantUserRecord> users, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, users, SerializerOptions, cancellationToken);
        }

        File.Move(temporaryPath, path, true);
    }

    public void Dispose()
    {
        gate.Dispose();
        GC.SuppressFinalize(this);
    }
}
