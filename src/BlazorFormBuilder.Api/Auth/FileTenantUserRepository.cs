using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlazorFormBuilder.Core.Auth;
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
    private readonly string invitationPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, "invitations.json");

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
                PasswordHash = string.Empty,
                Role = TenantRole.Owner
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

    public async ValueTask<IReadOnlyList<TenantMemberInfo>> ListMembersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadAsync(cancellationToken))
                .Where(user => user.TenantId == tenantId)
                .OrderBy(user => user.Email, StringComparer.OrdinalIgnoreCase)
                .Select(user => new TenantMemberInfo(user.Id, user.Email, user.Role))
                .ToArray();
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<IReadOnlyList<TenantInvitationInfo>> ListInvitationsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadInvitationsAsync(cancellationToken))
                .Where(invitation => invitation.TenantId == tenantId && invitation.AcceptedAtUtc is null)
                .OrderByDescending(invitation => invitation.ExpiresAtUtc)
                .Select(invitation => new TenantInvitationInfo(
                    invitation.Id,
                    invitation.Email,
                    invitation.Role,
                    invitation.ExpiresAtUtc))
                .ToArray();
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<TenantInvitationInfo?> CreateInvitationAsync(
        Guid tenantId,
        string email,
        TenantRole role,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (role is not (TenantRole.Editor or TenantRole.Viewer))
        {
            return null;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            var users = await LoadAsync(cancellationToken);
            var invitations = await LoadInvitationsAsync(cancellationToken);
            if (users.Any(user => user.TenantId == tenantId && string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)) ||
                invitations.Any(invitation => invitation.TenantId == tenantId && invitation.AcceptedAtUtc is null &&
                    invitation.ExpiresAtUtc > DateTimeOffset.UtcNow &&
                    string.Equals(invitation.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            var invitation = new TenantInvitationRecord
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = normalizedEmail,
                Role = role,
                TokenHash = HashToken(token),
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7)
            };
            invitations.Add(invitation);
            await SaveInvitationsAsync(invitations, cancellationToken);
            return new(invitation.Id, invitation.Email, invitation.Role, invitation.ExpiresAtUtc, token);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask<TenantUserRecord?> AcceptInvitationAsync(
        string token,
        string password,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token.Trim());
        await gate.WaitAsync(cancellationToken);
        try
        {
            var invitations = await LoadInvitationsAsync(cancellationToken);
            var invitation = invitations.FirstOrDefault(candidate =>
                candidate.AcceptedAtUtc is null &&
                candidate.ExpiresAtUtc > DateTimeOffset.UtcNow &&
                CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(candidate.TokenHash),
                    Convert.FromHexString(tokenHash)));
            if (invitation is null)
            {
                return null;
            }

            var users = await LoadAsync(cancellationToken);
            var tenantOwner = users.FirstOrDefault(user => user.TenantId == invitation.TenantId);
            if (tenantOwner is null || users.Any(user =>
                user.TenantId == invitation.TenantId &&
                string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var user = new TenantUserRecord
            {
                Id = Guid.NewGuid(),
                TenantId = invitation.TenantId,
                TenantName = tenantOwner.TenantName,
                TenantSlug = tenantOwner.TenantSlug,
                Email = invitation.Email,
                PasswordHash = string.Empty,
                Role = invitation.Role
            };
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            users.Add(user);
            invitation.AcceptedAtUtc = DateTimeOffset.UtcNow;
            await SaveAsync(users, cancellationToken);
            await SaveInvitationsAsync(invitations, cancellationToken);
            return user;
        }
        finally
        {
            gate.Release();
        }
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

    private async ValueTask<List<TenantInvitationRecord>> LoadInvitationsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(invitationPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(invitationPath);
        return await JsonSerializer.DeserializeAsync<List<TenantInvitationRecord>>(stream, SerializerOptions, cancellationToken) ?? [];
    }

    private async ValueTask SaveInvitationsAsync(
        List<TenantInvitationRecord> invitations,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(invitationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{invitationPath}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, invitations, SerializerOptions, cancellationToken);
        }

        File.Move(temporaryPath, invitationPath, true);
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public void Dispose()
    {
        gate.Dispose();
        GC.SuppressFinalize(this);
    }
}
