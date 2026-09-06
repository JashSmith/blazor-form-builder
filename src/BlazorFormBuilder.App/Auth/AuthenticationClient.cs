using System.Net;
using System.Net.Http.Json;
using BlazorFormBuilder.Core.Auth;

namespace BlazorFormBuilder.App.Auth;

public sealed class AuthenticationClient(HttpClient httpClient)
{
    public async ValueTask<SessionInfo?> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/auth/session", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionInfo>(cancellationToken);
    }

    public ValueTask<AuthenticationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync("api/auth/login", request, cancellationToken);

    public ValueTask<AuthenticationResult> RegisterAsync(
        RegisterTenantRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync("api/auth/register", request, cancellationToken);

    public async ValueTask LogoutAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync("api/auth/logout", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async ValueTask<AuthenticationResult> SendAsync<TRequest>(
        string uri,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(uri, request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return new(await response.Content.ReadFromJsonAsync<SessionInfo>(cancellationToken), null);
        }

        var error = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Email, password, or tenant is incorrect.",
            HttpStatusCode.Conflict => "That tenant or email already exists.",
            _ => "Authentication could not be completed. Check the entered values."
        };
        return new(null, error);
    }
}

public sealed record AuthenticationResult(SessionInfo? Session, string? Error);
