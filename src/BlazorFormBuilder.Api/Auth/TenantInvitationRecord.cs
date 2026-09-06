using BlazorFormBuilder.Core.Auth;

namespace BlazorFormBuilder.Api.Auth;

public sealed class TenantInvitationRecord
{
    public required Guid Id { get; init; }

    public required Guid TenantId { get; init; }

    public required string Email { get; init; }

    public required TenantRole Role { get; init; }

    public required string TokenHash { get; init; }

    public required DateTimeOffset ExpiresAtUtc { get; init; }

    public DateTimeOffset? AcceptedAtUtc { get; set; }
}
