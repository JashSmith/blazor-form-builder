using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Services;

namespace BlazorFormBuilder.Core.Tests;

public sealed class PageBuilderServiceTests
{
    [Fact]
    public void CreateWorkspaceAddsLandingPage()
    {
        var workspace = PageBuilderService.CreateWorkspace("Portal");

        var page = Assert.Single(workspace.Pages);
        Assert.Equal(page.Id, workspace.ActivePageId);
        Assert.Equal(3, page.Boxes.Count);
    }

    [Fact]
    public void AddPageCreatesUniqueSlug()
    {
        var workspace = PageBuilderService.CreateWorkspace("Portal");

        var page = PageBuilderService.AddPage(workspace, "Home");

        Assert.Equal("home-2", page.Slug);
    }

    [Fact]
    public void ApplySidebarTemplateCreatesThreeAndNineColumnBoxes()
    {
        var workspace = PageBuilderService.CreateWorkspace("Portal");
        var page = Assert.Single(workspace.Pages);

        PageBuilderService.ApplyTemplate(page, LayoutTemplateKind.Sidebar);

        Assert.Collection(
            page.Boxes,
            box => Assert.Equal(3, box.DesktopSpan),
            box => Assert.Equal(9, box.DesktopSpan));
    }

    [Fact]
    public void MoveBoxUpdatesOrder()
    {
        var page = Assert.Single(PageBuilderService.CreateWorkspace("Portal").Pages);
        var last = page.Boxes[^1];

        var moved = PageBuilderService.MoveBox(page, last.Id, 0);

        Assert.True(moved);
        Assert.Equal(last.Id, page.Boxes[0].Id);
        Assert.Collection(
            page.Boxes,
            box => Assert.Equal(0, box.Order),
            box => Assert.Equal(1, box.Order),
            box => Assert.Equal(2, box.Order));
    }

    [Fact]
    public void ClampGridConstrainsColumnsAndSpans()
    {
        var page = Assert.Single(PageBuilderService.CreateWorkspace("Portal").Pages);
        page.Grid.MobileColumns = 99;
        page.Boxes[0].MobileSpan = 99;

        PageBuilderService.ClampGrid(page);

        Assert.Equal(8, page.Grid.MobileColumns);
        Assert.Equal(8, page.Boxes[0].MobileSpan);
    }

    [Fact]
    public void MoveBoxBeforeLaterTargetKeepsDropPosition()
    {
        var page = Assert.Single(PageBuilderService.CreateWorkspace("Portal").Pages);
        var first = page.Boxes[0];
        var third = page.Boxes[2];

        PageBuilderService.MoveBox(page, first.Id, 2);

        Assert.Equal(first.Id, page.Boxes[1].Id);
        Assert.Equal(third.Id, page.Boxes[2].Id);
    }
}
