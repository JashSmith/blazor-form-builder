using BlazorFormBuilder.Api.Persistence;
using BlazorFormBuilder.Core.Models;
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

var app = builder.Build();
app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

MapDocumentEndpoints(app.MapGroup("/api/workspaces"), (BuilderWorkspaceDefinition document) => document.Id);
MapDocumentEndpoints(app.MapGroup("/api/forms"), (FormDefinition document) => document.Id);

app.MapFallbackToFile("index.html");
app.Run();

static void MapDocumentEndpoints<TDocument>(RouteGroupBuilder group, Func<TDocument, Guid> getId)
    where TDocument : class
{
    group.MapGet("/current", async Task<IResult> (
        HttpContext context,
        FileDocumentRepository<TDocument> repository,
        CancellationToken cancellationToken) =>
    {
        var stored = await repository.GetLatestAsync(cancellationToken);
        return stored is null ? Results.NotFound() : VersionedJson(context, stored);
    });

    group.MapGet("/{id:guid}", async Task<IResult> (
        Guid id,
        HttpContext context,
        FileDocumentRepository<TDocument> repository,
        CancellationToken cancellationToken) =>
    {
        var stored = await repository.GetAsync(id, cancellationToken);
        return stored is null ? Results.NotFound() : VersionedJson(context, stored);
    });

    group.MapPut("/{id:guid}", async Task<IResult> (
        Guid id,
        TDocument document,
        HttpContext context,
        FileDocumentRepository<TDocument> repository,
        CancellationToken cancellationToken) =>
    {
        if (id != getId(document))
        {
            return Results.BadRequest(new { error = "Route id must match document id." });
        }

        var expectedRevision = ParseRevision(context.Request.Headers.IfMatch);
        try
        {
            var stored = await repository.SaveAsync(document, expectedRevision, cancellationToken);
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
    });
}

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
