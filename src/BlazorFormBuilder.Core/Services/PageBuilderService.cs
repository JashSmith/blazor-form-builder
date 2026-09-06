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

        var header = AddHeader(workspace, "Main header");
        var footer = AddFooter(workspace, "Main footer");

        var page = AddPage(workspace, "Home");
        page.HeaderId = header.Id;
        page.FooterId = footer.Id;
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
            Slug = CreateUniqueSlug(workspace, normalizedName),
            HeaderId = workspace.Headers.FirstOrDefault()?.Id,
            FooterId = workspace.Footers.FirstOrDefault()?.Id
        };

        page.Title.Key = $"pages.{page.Slug}.title";
        page.Title.Values[workspace.Localization.DefaultLanguageCode] = normalizedName;
        page.LanguageCodes.Clear();
        page.LanguageCodes.Add(workspace.Localization.DefaultLanguageCode);

        workspace.Pages.Add(page);
        workspace.ActivePageId = page.Id;
        Touch(workspace);
        return page;
    }

    public static HeaderDefinition AddHeader(BuilderWorkspaceDefinition workspace, string name)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var header = new HeaderDefinition
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"Header {workspace.Headers.Count + 1}" : name.Trim()
        };
        header.LanguageCodes.Clear();
        header.LanguageCodes.Add(workspace.Localization.DefaultLanguageCode);
        header.Brand.Values[workspace.Localization.DefaultLanguageCode] = header.BrandText;
        foreach (var item in header.MenuItems)
        {
            item.LabelResource.Key = $"menu.{item.Id:N}.label";
            item.LabelResource.Values[workspace.Localization.DefaultLanguageCode] = item.Label;
        }
        workspace.Headers.Add(header);
        Touch(workspace);
        return header;
    }

    public static FooterDefinition AddFooter(BuilderWorkspaceDefinition workspace, string name)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var footer = new FooterDefinition
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"Footer {workspace.Footers.Count + 1}" : name.Trim()
        };
        footer.LanguageCodes.Clear();
        footer.LanguageCodes.Add(workspace.Localization.DefaultLanguageCode);
        footer.Copyright.Values[workspace.Localization.DefaultLanguageCode] = footer.CopyrightText;
        workspace.Footers.Add(footer);
        Touch(workspace);
        return footer;
    }

    public static void NormalizeWorkspace(BuilderWorkspaceDefinition workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        NormalizeLanguages(workspace.Localization);

        if (workspace.Headers.Count == 0)
        {
            foreach (var page in workspace.Pages)
            {
                var header = CloneLegacyHeader(page.Header, $"{page.Name} header", workspace.Localization.DefaultLanguageCode);
                workspace.Headers.Add(header);
                page.HeaderId = header.Id;
            }

            if (workspace.Headers.Count == 0)
            {
                AddHeader(workspace, "Main header");
            }
        }

        if (workspace.Footers.Count == 0)
        {
            foreach (var page in workspace.Pages)
            {
                var footer = CloneLegacyFooter(page.Footer, $"{page.Name} footer", workspace.Localization.DefaultLanguageCode);
                workspace.Footers.Add(footer);
                page.FooterId = footer.Id;
            }

            if (workspace.Footers.Count == 0)
            {
                AddFooter(workspace, "Main footer");
            }
        }

        foreach (var page in workspace.Pages)
        {
            page.HeaderId ??= workspace.Headers[0].Id;
            page.FooterId ??= workspace.Footers[0].Id;
            EnsureLanguageSelection(page.LanguageCodes, workspace.Localization.DefaultLanguageCode);
            if (string.IsNullOrWhiteSpace(page.Title.Key))
            {
                page.Title.Key = $"pages.{page.Slug}.title";
            }
            page.Title.Values.TryAdd(workspace.Localization.DefaultLanguageCode, page.Name);
        }

        foreach (var header in workspace.Headers)
        {
            EnsureLanguageSelection(header.LanguageCodes, workspace.Localization.DefaultLanguageCode);
            header.Brand.Values.TryAdd(workspace.Localization.DefaultLanguageCode, header.BrandText);
            foreach (var item in header.MenuItems)
            {
                item.LabelResource.Key = string.IsNullOrWhiteSpace(item.LabelResource.Key) ? $"menu.{item.Id:N}.label" : item.LabelResource.Key;
                item.LabelResource.Values.TryAdd(workspace.Localization.DefaultLanguageCode, item.Label);
            }
        }

        foreach (var footer in workspace.Footers)
        {
            EnsureLanguageSelection(footer.LanguageCodes, workspace.Localization.DefaultLanguageCode);
            footer.Copyright.Values.TryAdd(workspace.Localization.DefaultLanguageCode, footer.CopyrightText);
            EnsureFooterWidgetLabels(footer);
            foreach (var item in footer.Links)
            {
                item.LabelResource.Key = string.IsNullOrWhiteSpace(item.LabelResource.Key) ? $"footer.links.{item.Id:N}.label" : item.LabelResource.Key;
                item.LabelResource.Values.TryAdd(workspace.Localization.DefaultLanguageCode, item.Label);
            }
        }
    }

    public static void ToggleLanguage(List<string> languageCodes, string code, bool enabled, string defaultCode)
    {
        ArgumentNullException.ThrowIfNull(languageCodes);
        if (enabled && !languageCodes.Contains(code, StringComparer.OrdinalIgnoreCase))
        {
            languageCodes.Add(code);
        }
        else if (!enabled && !string.Equals(code, defaultCode, StringComparison.OrdinalIgnoreCase))
        {
            languageCodes.RemoveAll(item => string.Equals(item, code, StringComparison.OrdinalIgnoreCase));
        }

        EnsureLanguageSelection(languageCodes, defaultCode);
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

    private static void NormalizeLanguages(LocalizationDefinition localization)
    {
        if (localization.Languages.Count == 0)
        {
            localization.Languages.Add(new()
            {
                Code = "en",
                DisplayName = "English",
                Direction = ContentDirection.LeftToRight
            });
        }

        if (!localization.Languages.Any(language => string.Equals(language.Code, localization.DefaultLanguageCode, StringComparison.OrdinalIgnoreCase)))
        {
            localization.DefaultLanguageCode = localization.Languages[0].Code;
        }
    }

    private static void EnsureLanguageSelection(List<string> languageCodes, string defaultCode)
    {
        if (!languageCodes.Contains(defaultCode, StringComparer.OrdinalIgnoreCase))
        {
            languageCodes.Insert(0, defaultCode);
        }
    }

    private static void EnsureFooterWidgetLabels(FooterDefinition footer)
    {
        foreach (var widget in Enum.GetValues<FooterWidgetKind>())
        {
            if (footer.WidgetLabels.ContainsKey(widget))
            {
                continue;
            }

            footer.WidgetLabels[widget] = new()
            {
                Key = $"footer.widgets.{widget.ToString().ToLowerInvariant()}"
            };
            footer.WidgetLabels[widget].Values["en"] = widget switch
            {
                FooterWidgetKind.Messages => "0 messages",
                FooterWidgetKind.Logs => "Logs ready",
                FooterWidgetKind.Progress => "35%",
                FooterWidgetKind.Clock => "Clock",
                FooterWidgetKind.Connection => "Online",
                _ => widget.ToString()
            };
        }
    }

    private static HeaderDefinition CloneLegacyHeader(HeaderDefinition source, string name, string languageCode)
    {
        var header = new HeaderDefinition { Name = name, IsVisible = source.IsVisible, BrandText = source.BrandText };
        header.Brand.Values[languageCode] = source.BrandText;
        header.LanguageCodes.Clear();
        header.LanguageCodes.Add(languageCode);
        header.MenuItems.Clear();
        foreach (var item in source.MenuItems)
        {
            var clone = new NavigationItemDefinition { Id = Guid.NewGuid(), Label = item.Label, Url = item.Url };
            clone.LabelResource.Key = string.IsNullOrWhiteSpace(item.LabelResource.Key) ? $"menu.{clone.Id:N}.label" : item.LabelResource.Key;
            clone.LabelResource.Values[languageCode] = item.Label;
            header.MenuItems.Add(clone);
        }
        return header;
    }

    private static FooterDefinition CloneLegacyFooter(FooterDefinition source, string name, string languageCode)
    {
        var footer = new FooterDefinition { Name = name, IsVisible = source.IsVisible, CopyrightText = source.CopyrightText };
        footer.Copyright.Values[languageCode] = source.CopyrightText;
        footer.LanguageCodes.Clear();
        footer.LanguageCodes.Add(languageCode);
        footer.Widgets.Clear();
        footer.Widgets.UnionWith(source.Widgets);
        foreach (var item in source.Links)
        {
            var clone = new NavigationItemDefinition { Id = Guid.NewGuid(), Label = item.Label, Url = item.Url };
            clone.LabelResource.Key = string.IsNullOrWhiteSpace(item.LabelResource.Key) ? $"footer.links.{clone.Id:N}.label" : item.LabelResource.Key;
            clone.LabelResource.Values[languageCode] = item.Label;
            footer.Links.Add(clone);
        }
        return footer;
    }

    private static LayoutBoxDefinition CreateBox(PageDefinition page, LayoutBoxKind kind)
    {
        var title = kind switch
        {
            LayoutBoxKind.Hero => "Hero section",
            LayoutBoxKind.Form => "Form area",
            LayoutBoxKind.Sidebar => "Sidebar",
            LayoutBoxKind.Cards => "Card grid",
            LayoutBoxKind.Empty => "Empty container",
            _ => "Content section"
        };
        var box = new LayoutBoxDefinition
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            Title = title,
            DesktopSpan = page.Grid.DesktopColumns,
            TabletSpan = page.Grid.TabletColumns,
            MobileSpan = page.Grid.MobileColumns
        };
        box.TitleResource.Key = $"pages.{page.Id:N}.boxes.{box.Id:N}.title";
        box.TitleResource.Values[page.LanguageCodes.FirstOrDefault() ?? "en"] = title;
        return box;
    }

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
