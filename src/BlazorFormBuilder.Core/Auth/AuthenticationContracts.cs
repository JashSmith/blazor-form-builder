namespace BlazorFormBuilder.Core.Auth;

public enum TenantRole
{
    Owner,
    Editor,
    Viewer
}

public sealed record LoginRequest(string TenantSlug, string Email, string Password);

public sealed record RegisterTenantRequest(string TenantName, string Email, string Password);

public sealed record SessionInfo(Guid UserId, Guid TenantId, string TenantSlug, string Email, TenantRole Role);

public sealed record InviteTenantMemberRequest(string Email, TenantRole Role);

public sealed record AcceptTenantInvitationRequest(string Token, string Password);

public sealed record TenantMemberInfo(Guid UserId, string Email, TenantRole Role);

public sealed record TenantInvitationInfo(
    Guid InvitationId,
    string Email,
    TenantRole Role,
    DateTimeOffset ExpiresAtUtc,
    string? Token = null);
