using BlazorFormBuilder.Api.Auth;
using BlazorFormBuilder.Core.Auth;

namespace BlazorFormBuilder.Api.Tests;

public sealed class FileTenantUserRepositoryTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "blazor-form-builder-auth-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task RegistrationPersistsHashAndCredentialsCanBeValidated()
    {
        var path = Path.Combine(directory, "users.json");
        using var repository = new FileTenantUserRepository(path);

        var registered = await repository.RegisterTenantAsync("Acme Portal", "owner@acme.test", "correct-password");
        var authenticated = await repository.ValidateCredentialsAsync("acme-portal", "owner@acme.test", "correct-password");

        Assert.NotNull(registered);
        Assert.NotNull(authenticated);
        Assert.Equal(registered.TenantId, authenticated.TenantId);
        Assert.Equal(TenantRole.Owner, authenticated.Role);
        Assert.DoesNotContain("correct-password", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvitationTokenIsHashedSingleUseAndCreatesMemberWithAssignedRole()
    {
        var path = Path.Combine(directory, "users.json");
        using var repository = new FileTenantUserRepository(path);
        var owner = await repository.RegisterTenantAsync("Acme Portal", "owner@acme.test", "correct-password");

        var invitation = await repository.CreateInvitationAsync(
            owner!.TenantId,
            "editor@acme.test",
            TenantRole.Editor);
        var accepted = await repository.AcceptInvitationAsync(invitation!.Token!, "editor-password");
        var secondAcceptance = await repository.AcceptInvitationAsync(invitation.Token!, "another-password");

        Assert.NotNull(accepted);
        Assert.Equal(TenantRole.Editor, accepted.Role);
        Assert.Null(secondAcceptance);
        Assert.DoesNotContain(
            invitation.Token!,
            await File.ReadAllTextAsync(Path.Combine(directory, "invitations.json")),
            StringComparison.Ordinal);
        Assert.Equal(2, (await repository.ListMembersAsync(owner.TenantId)).Count);
        Assert.Empty(await repository.ListInvitationsAsync(owner.TenantId));
    }

    [Fact]
    public async Task ExistingTenantCannotBeRegisteredAgain()
    {
        using var repository = new FileTenantUserRepository(Path.Combine(directory, "users.json"));
        await repository.RegisterTenantAsync("Acme Portal", "owner@acme.test", "correct-password");

        var duplicate = await repository.RegisterTenantAsync("Acme Portal", "other@acme.test", "another-password");

        Assert.Null(duplicate);
    }

    [Fact]
    public async Task InvalidPasswordIsRejected()
    {
        using var repository = new FileTenantUserRepository(Path.Combine(directory, "users.json"));
        await repository.RegisterTenantAsync("Acme Portal", "owner@acme.test", "correct-password");

        var authenticated = await repository.ValidateCredentialsAsync("acme-portal", "owner@acme.test", "wrong-password");

        Assert.Null(authenticated);
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
