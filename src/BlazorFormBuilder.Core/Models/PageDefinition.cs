namespace BlazorFormBuilder.Core.Models;

public sealed class PageDefinition
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public ResponsiveGridDefinition Grid { get; init; } = new();

    public HeaderDefinition Header { get; init; } = new();

    public FooterDefinition Footer { get; init; } = new();

    public List<LayoutBoxDefinition> Boxes { get; init; } = [];
}

public sealed class ResponsiveGridDefinition
{
    public int DesktopColumns { get; set; } = 12;

    public int TabletColumns { get; set; } = 8;

    public int MobileColumns { get; set; } = 4;
}

public sealed class LayoutBoxDefinition
{
    public required Guid Id { get; init; }

    public required LayoutBoxKind Kind { get; init; }

    public required string Title { get; set; }

    public int Order { get; set; }

    public int DesktopSpan { get; set; } = 12;

    public int TabletSpan { get; set; } = 8;

    public int MobileSpan { get; set; } = 4;
}

public enum LayoutBoxKind
{
    Content,
    Hero,
    Form,
    Sidebar,
    Cards,
    Empty
}

public enum LayoutTemplateKind
{
    Blank,
    Landing,
    Dashboard,
    Sidebar
}

public enum PageViewport
{
    Desktop,
    Tablet,
    Mobile
}
