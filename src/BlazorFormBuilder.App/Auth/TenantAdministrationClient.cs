using System.Net.Http.Json;
using BlazorFormBuilder.Core.Auth;

namespace BlazorFormBuilder.App.Auth;

public sealed class TenantAdministrationClient(HttpClient httpClient)
{
    public async ValueTask<IReadOnlyList<TenantMemberInfo>> GetMembersAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<TenantMemberInfo[]>("api/tenant/members", cancellationToken) ?? [];

    public async ValueTask<IReadOnlyList<TenantInvitationInfo>> GetInvitationsAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<TenantInvitationInfo[]>("api/tenant/invitations", cancellationToken) ?? [];

    public async ValueTask<TenantInvitationInfo> InviteAsync(
        InviteTenantMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/tenant/invitations", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TenantInvitationInfo>(cancellationToken))!;
    }
}
