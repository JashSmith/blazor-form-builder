using BlazorFormBuilder.Api.Auth;

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
        Assert.DoesNotContain("correct-password", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
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
