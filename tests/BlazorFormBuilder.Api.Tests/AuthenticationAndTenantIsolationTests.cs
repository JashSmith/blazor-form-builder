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
}
