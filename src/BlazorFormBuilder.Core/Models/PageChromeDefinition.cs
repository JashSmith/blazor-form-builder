namespace BlazorFormBuilder.Core.Models;

public sealed class HeaderDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "Default header";

    public bool IsVisible { get; set; } = true;

    public string BrandText { get; set; } = "My application";

    public LocalizedTextDefinition Brand { get; init; } = new() { Key = "header.brand" };

    public List<string> LanguageCodes { get; init; } = ["en"];

    public List<NavigationItemDefinition> MenuItems { get; init; } =
    [
        new() { Id = Guid.NewGuid(), Label = "Home", Url = "/" }
    ];
}

public sealed class NavigationItemDefinition
{
    public required Guid Id { get; init; }

    public required string Label { get; set; }

    public LocalizedTextDefinition LabelResource { get; init; } = new();

    public required string Url { get; set; }
}

public sealed class FooterDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "Default footer";

    public bool IsVisible { get; set; } = true;

    public string CopyrightText { get; set; } = "Built with Blazor Form Builder";

    public LocalizedTextDefinition Copyright { get; init; } = new() { Key = "footer.copyright" };

    public List<string> LanguageCodes { get; init; } = ["en"];

    public List<NavigationItemDefinition> Links { get; init; } = [];

    public HashSet<FooterWidgetKind> Widgets { get; init; } =
    [
        FooterWidgetKind.Messages,
        FooterWidgetKind.Progress,
        FooterWidgetKind.Clock
    ];

    public Dictionary<FooterWidgetKind, LocalizedTextDefinition> WidgetLabels { get; init; } = new()
    {
        [FooterWidgetKind.Messages] = new() { Key = "footer.widgets.messages", Values = { ["en"] = "0 messages", ["fa"] = "۰ پیام" } },
        [FooterWidgetKind.Logs] = new() { Key = "footer.widgets.logs", Values = { ["en"] = "Logs ready", ["fa"] = "لاگ آماده" } },
        [FooterWidgetKind.Progress] = new() { Key = "footer.widgets.progress", Values = { ["en"] = "35%", ["fa"] = "۳۵٪" } },
        [FooterWidgetKind.Clock] = new() { Key = "footer.widgets.clock", Values = { ["en"] = "Clock", ["fa"] = "زمان" } },
        [FooterWidgetKind.Connection] = new() { Key = "footer.widgets.connection", Values = { ["en"] = "Online", ["fa"] = "آنلاین" } }
    };
}

public enum FooterWidgetKind
{
    Messages,
    Logs,
    Progress,
    Clock,
    Connection
}
