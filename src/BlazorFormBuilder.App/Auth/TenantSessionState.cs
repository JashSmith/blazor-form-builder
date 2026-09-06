using BlazorFormBuilder.Core.Auth;

namespace BlazorFormBuilder.App.Auth;

public sealed class TenantSessionState
{
    public Guid? TenantId { get; private set; }

    public TenantRole? Role { get; private set; }

    public void SetSession(SessionInfo session)
    {
        TenantId = session.TenantId;
        Role = session.Role;
    }

    public void Clear()
    {
        TenantId = null;
        Role = null;
    }

    public string StorageKey(string baseKey) => TenantId is { } tenantId
        ? $"{baseKey}:{tenantId:N}"
        : baseKey;
}
