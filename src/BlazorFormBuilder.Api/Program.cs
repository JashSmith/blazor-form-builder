using System.Security.Claims;
using BlazorFormBuilder.Api.Auth;
using BlazorFormBuilder.Api.Persistence;
using BlazorFormBuilder.Core.Auth;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Services;
using BlazorFormBuilder.Core.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);
var dataRoot = builder.Configuration["Storage:Path"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data");

builder.Services.AddSingleton(new FileDocumentRepository<BuilderWorkspaceDefinition>(
    Path.Combine(dataRoot, "workspaces"),
    document => document.Id));
builder.Services.AddSingleton(new FileDocumentRepository<FormDefinition>(
    Path.Combine(dataRoot, "forms"),
    document => document.Id));
builder.Services.AddSingleton(new FileDocumentRepository<WorkflowDefinition>(
    Path.Combine(dataRoot, "workflows"),
    document => document.Id));
builder.Services.AddSingleton(new FilePublishedFormRepository(Path.Combine(dataRoot, "published-forms")));
builder.Services.AddSingleton(new FileTenantUserRepository(Path.Combine(dataRoot, "identity", "users.json")));
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataRoot, "keys")))
    .SetApplicationName("BlazorFormBuilder");
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BlazorFormBuilder.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TenantOwner", policy => policy.RequireRole(nameof(TenantRole.Owner)));
    options.AddPolicy("TenantEditor", policy => policy.RequireRole(nameof(TenantRole.Owner), nameof(TenantRole.Editor)));
});

var app = builder.Build();
app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

MapAuthenticationEndpoints(app);
MapTenantAdministrationEndpoints(app);
MapWorkspaceEndpoints(app);
MapFormEndpoints(app);
MapPublicationEndpoints(app);
MapWorkflowEndpoints(app);

app.MapFallbackToFile("index.html");
app.Run();

static void MapWorkspaceEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/workspaces").RequireAuthorization();
    group.MapGet("/current", async Task<IResult> (
        HttpContext context,
        FileDocumentRepository<BuilderWorkspaceDefinition> repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(context.User);
        if (tenantId is null)
        {
            return Results.Unauthorized();
        }

        var stored = await repository.GetLatestAsync(tenantId.Value, cancellationToken);
        return stored is null ? Results.NotFound() : VersionedJson(context, stored);
    });

    group.MapGet("/{id:guid}", async Task<IResult> (
        Guid id,
        HttpContext context,
        FileDocumentRepository<BuilderWorkspaceDefinition> repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(context.User);
        if (tenantId is null)
        {
            return Results.Unauthorized();
        }

        var stored = await repository.GetAsync(tenantId.Value, id, cancellationToken);
        return stored is null ? Results.NotFound() : VersionedJson(context, stored);
    });

    group.MapPut("/{id:guid}", async Task<IResult> (
        Guid id,
        BuilderWorkspaceDefinition document,
        HttpContext context,
        FileDocumentRepository<BuilderWorkspaceDefinition> repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(context.User);
        if (tenantId is null)
        {
            return Results.Unauthorized();
        }

        if (id != document.Id)
        {
            return Results.BadRequest(new { error = "Route id must match document id." });
        }

        var expectedRevision = ParseRevision(context.Request.Headers.IfMatch);
        try
        {
            var stored = await repository.SaveAsync(tenantId.Value, document, expectedRevision, cancellationToken);
            return VersionedJson(
                context,
                stored,
                stored.Revision == 1 ? StatusCodes.Status201Created : StatusCodes.Status200OK);
        }
        catch (DocumentConcurrencyException exception)
        {
            return Results.Conflict(new
            {
                error = "The document was changed by another editor.",
                currentRevision = exception.CurrentRevision
            });
        }
    }).RequireAuthorization("TenantEditor");
}

static void MapFormEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/forms").RequireAuthorization();
    group.MapGet("/current", async Task<IResult> (
        HttpContext context,
        FileDocumentRepository<FormDefinition> repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(context.User);
        if (tenantId is null)
        {
            return Results.Unauthorized();
        }

        var stored = await repository.GetLatestAsync(tenantId.Value, cancellationToken);
        return stored is null ? Results.NotFound() : VersionedJson(context, stored);
    });

    group.MapGet("/{id:guid}", async Task<IResult> (
        Guid id,
        HttpContext context,
        FileDocumentRepository<FormDefinition> repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(context.User);
        if (tenantId is null)
        {
            return Results.Unauthorized();
        }

        var stored = await repository.GetAsync(tenantId.Value, id, cancellationToken);
        return stored is null ? Results.NotFound() : VersionedJson(context, stored);
    });

    group.MapPut("/{id:guid}", async Task<IResult> (
        Guid id,
        FormDefinition document,
        HttpContext context,
        FileDocumentRepository<FormDefinition> repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(context.User);
        if (tenantId is null)
        {
            return Results.Unauthorized();
        }

        if (id != document.Id)
        {
            return Results.BadRequest(new { error = "Route id must match document id." });
        }

        var expectedRevision = ParseRevision(context.Request.Headers.IfMatch);
        try
        {
            var stored = await repository.SaveAsync(tenantId.Value, document, expectedRevision, cancellationToken);
            return VersionedJson(
                context,
                stored,
                stored.Revision == 1 ? StatusCodes.Status201Created : StatusCodes.Status200OK);
        }
        catch (DocumentConcurrencyException exception)
        {
            return Results.Conflict(new
            {
                error = "The document was changed by another editor.",
                currentRevision = exception.CurrentRevision
            });
        }
    }).RequireAuthorization("TenantEditor");
}

static void MapPublicationEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/publications").RequireAuthorization();
    group.MapGet("/", async Task<IResult> (
        ClaimsPrincipal principal,
        FilePublishedFormRepository repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(principal);
        return tenantId is null
            ? Results.Unauthorized()
            : Results.Ok(await repository.ListAsync(tenantId.Value, cancellationToken));
    });

    group.MapGet("/forms/{formId:guid}/versions/{version:int}", async Task<IResult> (
        Guid formId,
        int version,
        ClaimsPrincipal principal,
        FilePublishedFormRepository repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(principal);
        if (tenantId is null)
        {
            return Results.Unauthorized();
        }
        var publication = await repository.GetAsync(tenantId.Value, formId, version, cancellationToken);
        return publication is null ? Results.NotFound() : Results.Ok(publication);
    });

    group.MapPost("/forms/{formId:guid}", async Task<IResult> (
        Guid formId,
        ClaimsPrincipal principal,
        FileDocumentRepository<FormDefinition> draftRepository,
        FilePublishedFormRepository publicationRepository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(principal);
        var userId = GetUserId(principal);
        if (tenantId is null || userId is null)
        {
            return Results.Unauthorized();
        }

        var draft = await draftRepository.GetAsync(tenantId.Value, formId, cancellationToken);
        if (draft is null)
        {
            return Results.NotFound(new { error = "Save the draft before publishing." });
        }
        var issues = FormDefinitionValidator.Validate(draft.Document);
        if (issues.Count > 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["form"] = issues.Select(issue => issue.Message).ToArray()
            });
        }

        var publication = await publicationRepository.PublishAsync(
            tenantId.Value,
            userId.Value,
            draft.Document,
            cancellationToken);
        return Results.Created(
            $"/api/publications/forms/{formId}/versions/{publication.Version}",
            publication);
    }).RequireAuthorization("TenantEditor");
}

static void MapWorkflowEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/workflows").RequireAuthorization();
    group.MapGet("/current", async Task<IResult> (
        HttpContext context,
        FileDocumentRepository<WorkflowDefinition> repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(context.User);
        if (tenantId is null)
        {
            return Results.Unauthorized();
        }
        var stored = await repository.GetLatestAsync(tenantId.Value, cancellationToken);
        return stored is null ? Results.NotFound() : VersionedJson(context, stored);
    });

    group.MapPut("/{id:guid}", async Task<IResult> (
        Guid id,
        WorkflowDefinition workflow,
        HttpContext context,
        FileDocumentRepository<WorkflowDefinition> repository,
        FilePublishedFormRepository publicationRepository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(context.User);
        if (tenantId is null)
        {
            return Results.Unauthorized();
        }
        if (id != workflow.Id)
        {
            return Results.BadRequest(new { error = "Route id must match workflow id." });
        }

        var issues = WorkflowDefinitionService.Validate(workflow).ToList();
        foreach (var task in workflow.UserTasks.Where(task => task.FormId is not null && task.FormVersion is not null))
        {
            var publication = await publicationRepository.GetAsync(
                tenantId.Value,
                task.FormId!.Value,
                task.FormVersion!.Value,
                cancellationToken);
            if (publication is null)
            {
                issues.Add($"User task '{task.Name}' references a published form version that does not exist.");
            }
        }
        if (issues.Count > 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["workflow"] = issues.ToArray() });
        }

        WorkflowDefinitionService.Touch(workflow);
        var expectedRevision = ParseRevision(context.Request.Headers.IfMatch);
        try
        {
            var stored = await repository.SaveAsync(tenantId.Value, workflow, expectedRevision, cancellationToken);
            return VersionedJson(
                context,
                stored,
                stored.Revision == 1 ? StatusCodes.Status201Created : StatusCodes.Status200OK);
        }
        catch (DocumentConcurrencyException exception)
        {
            return Results.Conflict(new
            {
                error = "The workflow was changed by another editor.",
                currentRevision = exception.CurrentRevision
            });
        }
    }).RequireAuthorization("TenantEditor");
}

static void MapAuthenticationEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/auth");
    group.MapPost("/register", async Task<IResult> (
        RegisterTenantRequest request,
        HttpContext context,
        FileTenantUserRepository repository,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.TenantName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["registration"] = ["Tenant, email, and a password of at least 8 characters are required."]
            });
        }

        var user = await repository.RegisterTenantAsync(
            request.TenantName,
            request.Email,
            request.Password,
            cancellationToken);
        if (user is null)
        {
            return Results.Conflict(new { error = "Tenant or email already exists." });
        }

        await context.SignInAsync(CreatePrincipal(user));
        return Results.Ok(ToSession(user));
    }).AllowAnonymous();

    group.MapPost("/login", async Task<IResult> (
        LoginRequest request,
        HttpContext context,
        FileTenantUserRepository repository,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.TenantSlug) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.Unauthorized();
        }

        var user = await repository.ValidateCredentialsAsync(
            request.TenantSlug,
            request.Email,
            request.Password,
            cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        await context.SignInAsync(CreatePrincipal(user));
        return Results.Ok(ToSession(user));
    }).AllowAnonymous();

    group.MapPost("/accept-invitation", async Task<IResult> (
        AcceptTenantInvitationRequest request,
        HttpContext context,
        FileTenantUserRepository repository,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8)
        {
            return Results.BadRequest(new { error = "A valid invitation and password are required." });
        }

        var user = await repository.AcceptInvitationAsync(request.Token, request.Password, cancellationToken);
        if (user is null)
        {
            return Results.BadRequest(new { error = "Invitation is invalid, expired, or already used." });
        }

        await context.SignInAsync(CreatePrincipal(user));
        return Results.Ok(ToSession(user));
    }).AllowAnonymous();

    group.MapGet("/session", (ClaimsPrincipal user) =>
    {
        var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var tenantId = Guid.Parse(user.FindFirstValue("tenant_id")!);
        return Results.Ok(new SessionInfo(
            userId,
            tenantId,
            user.FindFirstValue("tenant_slug")!,
            user.FindFirstValue(ClaimTypes.Email)!,
            Enum.Parse<TenantRole>(user.FindFirstValue(ClaimTypes.Role)!)));
    }).RequireAuthorization();

    group.MapPost("/logout", async (HttpContext context) =>
    {
        await context.SignOutAsync();
        return Results.NoContent();
    }).RequireAuthorization();
}

static void MapTenantAdministrationEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/tenant").RequireAuthorization("TenantOwner");
    group.MapGet("/members", async Task<IResult> (
        ClaimsPrincipal principal,
        FileTenantUserRepository repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(principal);
        return tenantId is null
            ? Results.Unauthorized()
            : Results.Ok(await repository.ListMembersAsync(tenantId.Value, cancellationToken));
    });

    group.MapGet("/invitations", async Task<IResult> (
        ClaimsPrincipal principal,
        FileTenantUserRepository repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(principal);
        return tenantId is null
            ? Results.Unauthorized()
            : Results.Ok(await repository.ListInvitationsAsync(tenantId.Value, cancellationToken));
    });

    group.MapPost("/invitations", async Task<IResult> (
        InviteTenantMemberRequest request,
        ClaimsPrincipal principal,
        FileTenantUserRepository repository,
        CancellationToken cancellationToken) =>
    {
        var tenantId = GetTenantId(principal);
        if (tenantId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Email) ||
            request.Role is not (TenantRole.Editor or TenantRole.Viewer))
        {
            return Results.BadRequest(new { error = "Invite an Editor or Viewer with a valid email." });
        }

        var invitation = await repository.CreateInvitationAsync(
            tenantId.Value,
            request.Email,
            request.Role,
            cancellationToken);
        return invitation is null
            ? Results.Conflict(new { error = "Member or active invitation already exists." })
            : Results.Created($"/api/tenant/invitations/{invitation.InvitationId}", invitation);
    });
}

static ClaimsPrincipal CreatePrincipal(TenantUserRecord user)
{
    var identity = new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("tenant_id", user.TenantId.ToString()),
        new Claim("tenant_slug", user.TenantSlug)
    ], CookieAuthenticationDefaults.AuthenticationScheme);
    return new ClaimsPrincipal(identity);
}

static SessionInfo ToSession(TenantUserRecord user) =>
    new(user.Id, user.TenantId, user.TenantSlug, user.Email, user.Role);

static Guid? GetTenantId(ClaimsPrincipal user) =>
    Guid.TryParse(user.FindFirstValue("tenant_id"), out var tenantId) ? tenantId : null;

static Guid? GetUserId(ClaimsPrincipal user) =>
    Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

static IResult VersionedJson<TDocument>(
    HttpContext context,
    StoredDocument<TDocument> stored,
    int statusCode = StatusCodes.Status200OK)
{
    context.Response.Headers.ETag = $"\"{stored.Revision}\"";
    return Results.Json(stored.Document, statusCode: statusCode);
}

static long? ParseRevision(StringValues values)
{
    var value = values.Count > 0 ? values[0]?.Trim().Trim('"') : null;
    return long.TryParse(value, out var revision) ? revision : null;
}

public partial class Program
{
}
