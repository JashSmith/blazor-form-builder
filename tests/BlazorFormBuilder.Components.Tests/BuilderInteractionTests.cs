using BlazorFormBuilder.Components;
using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Storage;
using BlazorFormBuilder.Plugins.Standard;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorFormBuilder.Components.Tests;

public sealed class BuilderInteractionTests : BunitContext
{
    private readonly MemoryFormStore formStore = new();
    private readonly MemoryWorkspaceStore workspaceStore = new();

    public BuilderInteractionTests()
    {
        Services.AddSingleton<IFormDefinitionStore>(formStore);
        Services.AddSingleton<IBuilderWorkspaceStore>(workspaceStore);
        Services.AddStandardFormFieldPlugins();
    }

    [Fact]
    public void WorkspaceNavigationSwitchesBetweenBuilders()
    {
        var component = Render<BuilderWorkspace>();

        component.Find("[data-testid='open-form-builder']").Click();
        Assert.Contains("Form designer", component.Markup, StringComparison.Ordinal);

        component.Find("[data-testid='open-page-builder']").Click();
        Assert.Contains("Page builder", component.Markup, StringComparison.OrdinalIgnoreCase);

        component.Find("[data-testid='open-header-builder']").Click();
        Assert.Contains("HEADER &amp; MENU LIBRARY", component.Markup, StringComparison.Ordinal);

        component.Find("[data-testid='open-footer-builder']").Click();
        Assert.Contains("FOOTER LIBRARY", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void CreatePageButtonAddsAndSelectsPage()
    {
        var component = Render<PageDesigner>();

        component.Find("[data-testid='create-page']").Click();

        Assert.Equal(2, component.FindAll(".page-list > button").Count);
        Assert.Contains("Page 2", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ViewportButtonsChangeResponsiveCanvas()
    {
        var component = Render<PageDesigner>();

        component.Find("[data-testid='viewport-mobile']").Click();

        Assert.Contains("390px · 4 columns", component.Markup, StringComparison.Ordinal);
        Assert.Contains("page-canvas mobile", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void LayoutTemplateButtonReplacesCanvasBoxes()
    {
        var component = Render<PageDesigner>();

        component.Find("[data-template='Sidebar']").Click();

        Assert.Equal(2, component.FindAll(".layout-box").Count);
        Assert.Contains("Sidebar", component.Markup, StringComparison.Ordinal);
        Assert.Contains("Content section", component.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceSavePersistsCurrentDefinition()
    {
        var component = Render<PageDesigner>();
        component.Find("[data-testid='create-page']").Click();

        component.Find("[data-testid='save-workspace']").Click();

        Assert.Equal(1, workspaceStore.SaveCount);
        Assert.NotNull(workspaceStore.SavedWorkspace);
        Assert.Equal(2, workspaceStore.SavedWorkspace.Pages.Count);
    }

    [Fact]
    public void FormToolboxAndPreviewButtonsAreInteractive()
    {
        var component = Render<FormDesigner>();

        component.Find("[data-field-type='email']").Click();
        Assert.Single(component.FindAll(".field-card"));

        component.Find("[data-testid='toggle-form-preview']").Click();
        Assert.Contains("LIVE FORM", component.Markup, StringComparison.Ordinal);
        Assert.Single(component.FindAll("input[type='email']"));
    }

    [Fact]
    public void HeaderLibraryCreatesAndPersistsReusableVariant()
    {
        var component = Render<ChromeDesigner>(parameters => parameters.Add(item => item.Kind, ChromeBuilderKind.Header));

        component.Find("[data-testid='create-chrome']").Click();
        component.Find("[data-testid='add-menu-item']").Click();
        component.Find("[data-testid='save-chrome']").Click();

        Assert.Equal(2, component.FindAll(".definition-list > button").Count);
        Assert.Equal(2, workspaceStore.SavedWorkspace?.Headers.Count);
        Assert.Equal(2, workspaceStore.SavedWorkspace?.Headers.Last().MenuItems.Count);
    }

    [Fact]
    public void PagePreviewSwitchesToRtlForPersian()
    {
        var component = Render<PageDesigner>();

        component.Find("[data-testid='page-languages'] input:not([disabled])").Change(true);
        component.Find("select[aria-label='Page preview language']").Change("fa");

        Assert.Contains("dir=\"rtl\"", component.Find(".page-canvas").OuterHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void FormCollectsTranslationKeyAndRtlValues()
    {
        var component = Render<FormDesigner>();

        component.Find("[data-testid='form-languages'] input:not([disabled])").Change(true);
        component.Find("input[placeholder='example.section.title']").Input("forms.customer.title");
        component.Find("[data-testid='toggle-form-preview']").Click();
        component.Find("select[aria-label='Form preview language']").Change("fa");

        Assert.Contains("dir=\"rtl\"", component.Find(".preview-card").OuterHtml, StringComparison.Ordinal);
    }

    private sealed class MemoryFormStore : IFormDefinitionStore
    {
        public FormDefinition? SavedForm { get; private set; }

        public ValueTask<FormDefinition?> LoadAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<FormDefinition?>(null);

        public ValueTask SaveAsync(FormDefinition definition, CancellationToken cancellationToken = default)
        {
            SavedForm = definition;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MemoryWorkspaceStore : IBuilderWorkspaceStore
    {
        public int SaveCount { get; private set; }

        public BuilderWorkspaceDefinition? SavedWorkspace { get; private set; }

        public ValueTask<BuilderWorkspaceDefinition?> LoadAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<BuilderWorkspaceDefinition?>(null);

        public ValueTask SaveAsync(BuilderWorkspaceDefinition workspace, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            SavedWorkspace = workspace;
            return ValueTask.CompletedTask;
        }
    }
}
