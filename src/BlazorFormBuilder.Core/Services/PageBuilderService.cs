using System.Text.RegularExpressions;
using BlazorFormBuilder.Core.Models;

namespace BlazorFormBuilder.Core.Services;

public static partial class PageBuilderService
{
    public static BuilderWorkspaceDefinition CreateWorkspace(string name)
    {
        var workspace = new BuilderWorkspaceDefinition
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(name) ? "Untitled application" : name.Trim()
        };

        var page = AddPage(workspace, "Home");
        ApplyTemplate(page, LayoutTemplateKind.Landing);
        return workspace;
    }

    public static PageDefinition AddPage(BuilderWorkspaceDefinition workspace, string name)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var normalizedName = string.IsNullOrWhiteSpace(name) ? $"Page {workspace.Pages.Count + 1}" : name.Trim();
        var page = new PageDefinition
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Slug = CreateUniqueSlug(workspace, normalizedName)
        };

        workspace.Pages.Add(page);
        workspace.ActivePageId = page.Id;
        Touch(workspace);
        return page;
    }

    public static LayoutBoxDefinition AddBox(PageDefinition page, LayoutBoxKind kind, int? index = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        var box = CreateBox(page, kind);
        var target = Math.Clamp(index ?? page.Boxes.Count, 0, page.Boxes.Count);
        page.Boxes.Insert(target, box);
        NormalizeOrder(page);
        return box;
    }

    public static bool MoveBox(PageDefinition page, Guid boxId, int targetIndex)
    {
        ArgumentNullException.ThrowIfNull(page);
        var sourceIndex = page.Boxes.FindIndex(item => item.Id == boxId);
        if (sourceIndex < 0 || page.Boxes.Count == 0)
        {
            return false;
        }

        var box = page.Boxes[sourceIndex];
        page.Boxes.RemoveAt(sourceIndex);
        if (sourceIndex < targetIndex)
        {
            targetIndex--;
        }

        page.Boxes.Insert(Math.Clamp(targetIndex, 0, page.Boxes.Count), box);
        NormalizeOrder(page);
        return true;
    }

    public static void RemoveBox(PageDefinition page, Guid boxId)
    {
        ArgumentNullException.ThrowIfNull(page);
        page.Boxes.RemoveAll(item => item.Id == boxId);
        NormalizeOrder(page);
    }

    public static void ApplyTemplate(PageDefinition page, LayoutTemplateKind template)
    {
        ArgumentNullException.ThrowIfNull(page);
        page.Boxes.Clear();

        switch (template)
        {
            case LayoutTemplateKind.Blank:
                break;
            case LayoutTemplateKind.Landing:
                AddBox(page, LayoutBoxKind.Hero);
                AddBox(page, LayoutBoxKind.Cards);
                AddBox(page, LayoutBoxKind.Form);
                break;
            case LayoutTemplateKind.Dashboard:
                AddBox(page, LayoutBoxKind.Cards);
                AddBox(page, LayoutBoxKind.Content);
                AddBox(page, LayoutBoxKind.Content);
                page.Boxes[1].DesktopSpan = 6;
                page.Boxes[2].DesktopSpan = 6;
                break;
            case LayoutTemplateKind.Sidebar:
                AddBox(page, LayoutBoxKind.Sidebar);
                AddBox(page, LayoutBoxKind.Content);
                page.Boxes[0].DesktopSpan = 3;
                page.Boxes[1].DesktopSpan = 9;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(template), template, "Unknown page template.");
        }

        NormalizeOrder(page);
    }

    public static void ClampGrid(PageDefinition page)
    {
        ArgumentNullException.ThrowIfNull(page);
        page.Grid.DesktopColumns = Math.Clamp(page.Grid.DesktopColumns, 1, 24);
        page.Grid.TabletColumns = Math.Clamp(page.Grid.TabletColumns, 1, 16);
        page.Grid.MobileColumns = Math.Clamp(page.Grid.MobileColumns, 1, 8);

        foreach (var box in page.Boxes)
        {
            box.DesktopSpan = Math.Clamp(box.DesktopSpan, 1, page.Grid.DesktopColumns);
            box.TabletSpan = Math.Clamp(box.TabletSpan, 1, page.Grid.TabletColumns);
            box.MobileSpan = Math.Clamp(box.MobileSpan, 1, page.Grid.MobileColumns);
        }
    }

    public static void Touch(BuilderWorkspaceDefinition workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        workspace.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static LayoutBoxDefinition CreateBox(PageDefinition page, LayoutBoxKind kind) => new()
    {
        Id = Guid.NewGuid(),
        Kind = kind,
        Title = kind switch
        {
            LayoutBoxKind.Hero => "Hero section",
            LayoutBoxKind.Form => "Form area",
            LayoutBoxKind.Sidebar => "Sidebar",
            LayoutBoxKind.Cards => "Card grid",
            LayoutBoxKind.Empty => "Empty container",
            _ => "Content section"
        },
        DesktopSpan = page.Grid.DesktopColumns,
        TabletSpan = page.Grid.TabletColumns,
        MobileSpan = page.Grid.MobileColumns
    };

    private static string CreateUniqueSlug(BuilderWorkspaceDefinition workspace, string name)
    {
        var slug = NonSlugCharacters().Replace(name.Trim().ToLowerInvariant(), "-").Trim('-');
        slug = string.IsNullOrWhiteSpace(slug) ? "page" : slug;
        var candidate = slug;
        var suffix = 1;

        while (workspace.Pages.Any(page => string.Equals(page.Slug, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{slug}-{++suffix}";
        }

        return candidate;
    }

    private static void NormalizeOrder(PageDefinition page)
    {
        for (var index = 0; index < page.Boxes.Count; index++)
        {
            page.Boxes[index].Order = index;
        }
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonSlugCharacters();
}
