namespace BlazorFormBuilder.Core.Auth;

public sealed record LoginRequest(string TenantSlug, string Email, string Password);

public sealed record RegisterTenantRequest(string TenantName, string Email, string Password);

public sealed record SessionInfo(Guid UserId, Guid TenantId, string TenantSlug, string Email);
