namespace BlazorFormBuilder.Api.Auth;

public sealed class TenantUserRecord
{
    public required Guid Id { get; init; }

    public required Guid TenantId { get; init; }

    public required string TenantName { get; init; }

    public required string TenantSlug { get; init; }

    public required string Email { get; init; }

    public required string PasswordHash { get; set; }
}
