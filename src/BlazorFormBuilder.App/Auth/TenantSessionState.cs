namespace BlazorFormBuilder.App.Auth;

public sealed class TenantSessionState
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid tenantId) => TenantId = tenantId;

    public void Clear() => TenantId = null;

    public string StorageKey(string baseKey) => TenantId is { } tenantId
        ? $"{baseKey}:{tenantId:N}"
        : baseKey;
}
