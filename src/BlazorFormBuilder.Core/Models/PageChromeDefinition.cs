namespace BlazorFormBuilder.Core.Models;

public sealed class HeaderDefinition
{
    public bool IsVisible { get; set; } = true;

    public string BrandText { get; set; } = "My application";

    public List<NavigationItemDefinition> MenuItems { get; init; } =
    [
        new() { Id = Guid.NewGuid(), Label = "Home", Url = "/" }
    ];
}

public sealed class NavigationItemDefinition
{
    public required Guid Id { get; init; }

    public required string Label { get; set; }

    public required string Url { get; set; }
}

public sealed class FooterDefinition
{
    public bool IsVisible { get; set; } = true;

    public string CopyrightText { get; set; } = "Built with Blazor Form Builder";

    public List<NavigationItemDefinition> Links { get; init; } = [];

    public HashSet<FooterWidgetKind> Widgets { get; init; } =
    [
        FooterWidgetKind.Messages,
        FooterWidgetKind.Progress,
        FooterWidgetKind.Clock
    ];
}

public enum FooterWidgetKind
{
    Messages,
    Logs,
    Progress,
    Clock,
    Connection
}
