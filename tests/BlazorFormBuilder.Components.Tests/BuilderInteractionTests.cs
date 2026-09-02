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
