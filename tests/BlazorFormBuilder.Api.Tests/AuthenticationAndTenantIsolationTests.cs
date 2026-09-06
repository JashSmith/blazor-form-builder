using System.Net;
using System.Net.Http.Json;
using BlazorFormBuilder.Core.Auth;
using BlazorFormBuilder.Core.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BlazorFormBuilder.Api.Tests;

public sealed class AuthenticationAndTenantIsolationTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "blazor-form-builder-host-tests",
        Guid.NewGuid().ToString("N"));
    private readonly WebApplicationFactory<Program> factory;

    public AuthenticationAndTenantIsolationTests()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("Storage:Path", directory));
    }

    [Fact]
    public async Task AnonymousDocumentRequestIsRejected()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/workspaces/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedTenantCannotReadAnotherTenantsWorkspace()
    {
        using var firstTenantClient = factory.CreateClient();
        using var secondTenantClient = factory.CreateClient();
        await RegisterAsync(firstTenantClient, "Tenant One", "owner@one.test");
        await RegisterAsync(secondTenantClient, "Tenant Two", "owner@two.test");

        var workspace = PageBuilderService.CreateWorkspace("Private portal");
        using var saveResponse = await firstTenantClient.PutAsJsonAsync($"/api/workspaces/{workspace.Id}", workspace);
        using var crossTenantResponse = await secondTenantClient.GetAsync($"/api/workspaces/{workspace.Id}");

        Assert.Equal(HttpStatusCode.Created, saveResponse.StatusCode);
        Assert.NotNull(saveResponse.Headers.ETag);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantResponse.StatusCode);
    }

    [Fact]
    public async Task OwnerCanInviteEditorWhoCanSaveButCannotManageTeam()
    {
        using var ownerClient = factory.CreateClient();
        using var editorClient = factory.CreateClient();
        await RegisterAsync(ownerClient, "Editorial Team", "owner@editorial.test");

        var invitation = await InviteAsync(ownerClient, "editor@editorial.test", TenantRole.Editor);
        using var acceptResponse = await editorClient.PostAsJsonAsync(
            "/api/auth/accept-invitation",
            new AcceptTenantInvitationRequest(invitation.Token!, "editor-password"));
        var session = await acceptResponse.Content.ReadFromJsonAsync<SessionInfo>();
        var workspace = PageBuilderService.CreateWorkspace("Shared workspace");
        using var saveResponse = await editorClient.PutAsJsonAsync($"/api/workspaces/{workspace.Id}", workspace);
        using var teamResponse = await editorClient.GetAsync("/api/tenant/members");

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        Assert.Equal(TenantRole.Editor, session!.Role);
        Assert.Equal(HttpStatusCode.Created, saveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, teamResponse.StatusCode);
    }

    [Fact]
    public async Task ViewerCanReadWorkspaceButCannotSave()
    {
        using var ownerClient = factory.CreateClient();
        using var viewerClient = factory.CreateClient();
        await RegisterAsync(ownerClient, "Review Team", "owner@review.test");
        var workspace = PageBuilderService.CreateWorkspace("Review workspace");
        using var ownerSaveResponse = await ownerClient.PutAsJsonAsync($"/api/workspaces/{workspace.Id}", workspace);
        ownerSaveResponse.EnsureSuccessStatusCode();

        var invitation = await InviteAsync(ownerClient, "viewer@review.test", TenantRole.Viewer);
        using var acceptResponse = await viewerClient.PostAsJsonAsync(
            "/api/auth/accept-invitation",
            new AcceptTenantInvitationRequest(invitation.Token!, "viewer-password"));
        acceptResponse.EnsureSuccessStatusCode();
        using var readResponse = await viewerClient.GetAsync("/api/workspaces/current");
        using var saveResponse = await viewerClient.PutAsJsonAsync($"/api/workspaces/{workspace.Id}", workspace);

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, saveResponse.StatusCode);
    }

    public void Dispose()
    {
        factory.Dispose();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private static async Task RegisterAsync(HttpClient client, string tenantName, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterTenantRequest(tenantName, email, "testing-password"));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<TenantInvitationInfo> InviteAsync(
        HttpClient ownerClient,
        string email,
        TenantRole role)
    {
        using var response = await ownerClient.PostAsJsonAsync(
            "/api/tenant/invitations",
            new InviteTenantMemberRequest(email, role));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TenantInvitationInfo>())!;
    }
}
