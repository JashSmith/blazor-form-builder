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
    private readonly MemoryPublishedFormStore publicationStore;
    private readonly MemoryWorkflowStore workflowStore = new();

    public BuilderInteractionTests()
    {
        publicationStore = new(formStore);
        Services.AddSingleton<IFormDefinitionStore>(formStore);
        Services.AddSingleton<IBuilderWorkspaceStore>(workspaceStore);
        Services.AddSingleton<IPublishedFormStore>(publicationStore);
        Services.AddSingleton<IWorkflowDefinitionStore>(workflowStore);
        Services.AddStandardFormFieldPlugins();
    }

    [Fact]
    public void WorkspaceNavigationSwitchesBetweenBuilders()
    {
        var component = Render<BuilderWorkspace>();

        component.Find("[data-testid='open-form-builder']").Click();
        Assert.Contains("Form designer", component.Markup, StringComparison.Ordinal);

        component.Find("[data-testid='open-page-builder']").Click();
        component.WaitForAssertion(() =>
        {
            Assert.True(component.Find("[data-testid='open-page-builder']").ClassList.Contains("active"));
            Assert.NotNull(component.FindComponent<PageDesigner>());
        });

        component.Find("[data-testid='open-header-builder']").Click();
        component.WaitForAssertion(() =>
            Assert.Contains("HEADER &amp; MENU LIBRARY", component.Markup, StringComparison.Ordinal));

        component.Find("[data-testid='open-footer-builder']").Click();
        component.WaitForAssertion(() =>
            Assert.Contains("FOOTER LIBRARY", component.Markup, StringComparison.Ordinal));

        component.Find("[data-testid='open-workflow-builder']").Click();
        component.WaitForAssertion(() =>
            Assert.NotNull(component.FindComponent<WorkflowDesigner>()));
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
        component.Find("[data-testid='chrome-next']").Click();
        component.Find("[data-testid='add-menu-item']").Click();
        component.Find("[data-testid='save-chrome']").Click();
        component.Find("[data-testid='chrome-step-0']").Click();

        Assert.Equal(2, component.FindAll(".definition-list > button").Count);
        Assert.Equal(2, workspaceStore.SavedWorkspace?.Headers.Count);
        Assert.Equal(2, workspaceStore.SavedWorkspace?.Headers.Last().MenuItems.Count);
    }

    [Fact]
    public void FooterPersianTranslationUpdatesRtlPreview()
    {
        var component = Render<ChromeDesigner>(parameters => parameters.Add(item => item.Kind, ChromeBuilderKind.Footer));

        component.Find("[data-testid='chrome-step-2']").Click();
        component.Find("[data-testid='footer-languages'] input:not([disabled])").Change(true);
        component.Find("select[aria-label='Preview language']").Change("fa");
        component.FindAll(".localized-editor input[dir='rtl']")[0].Input("تمام حقوق محفوظ است");

        var preview = component.Find("[data-testid='chrome-preview']");
        Assert.Contains("dir=\"rtl\"", preview.OuterHtml, StringComparison.Ordinal);
        Assert.Contains("تمام حقوق محفوظ است", preview.TextContent, StringComparison.Ordinal);
        Assert.Contains("۰ پیام", preview.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void GuidedPageStepsShowFocusedControls()
    {
        var component = Render<PageDesigner>();

        component.Find("[data-testid='page-step-1']").Click();
        Assert.Contains("aria-current=\"step\"", component.Find("[data-testid='page-step-1']").OuterHtml, StringComparison.Ordinal);

        component.Find("[data-testid='page-step-2']").Click();
        Assert.Contains("Step 3 of 4", component.Markup, StringComparison.Ordinal);
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

    [Fact]
    public void ValidFormCanPublishAnImmutableVersion()
    {
        var component = Render<FormDesigner>();
        component.Find("[data-field-type='email']").Click();

        component.Find("[data-testid='publish-form']").Click();

        component.WaitForAssertion(() =>
            Assert.Contains("Published immutable version 1", component.Markup, StringComparison.Ordinal));
        Assert.Single(publicationStore.Publications, item => item.FormId == formStore.SavedForm!.Id);
    }

    [Fact]
    public void WorkflowTaskStoresTheSelectedPublishedFormVersion()
    {
        var component = Render<WorkflowDesigner>();
        var publication = publicationStore.Publications[0];
        component.Find("[data-testid='add-user-task']").Click();

        component.Find("[data-testid='task-publication']").Change(publication.PublicationId.ToString());
        component.Find("[data-testid='save-workflow']").Click();

        component.WaitForAssertion(() =>
        {
            Assert.NotNull(workflowStore.SavedWorkflow);
            Assert.Equal(publication.FormId, workflowStore.SavedWorkflow.UserTasks[0].FormId);
            Assert.Equal(publication.Version, workflowStore.SavedWorkflow.UserTasks[0].FormVersion);
        });
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

    private sealed class MemoryPublishedFormStore(MemoryFormStore formStore) : IPublishedFormStore
    {
        public List<PublishedFormVersion> Publications { get; } =
        [
            CreatePublication(
                new FormDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "Existing form",
                    Fields =
                    [
                        new() { Id = Guid.NewGuid(), Type = "text", Key = "name", Label = "Name" }
                    ]
                },
                1)
        ];

        public ValueTask<IReadOnlyList<PublishedFormVersion>> ListAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<PublishedFormVersion>>(Publications);

        public ValueTask<PublishedFormVersion> PublishAsync(
            Guid formId,
            CancellationToken cancellationToken = default)
        {
            var publication = CreatePublication(
                formStore.SavedForm!,
                Publications.Count(item => item.FormId == formId) + 1);
            Publications.Add(publication);
            return ValueTask.FromResult(publication);
        }

        private static PublishedFormVersion CreatePublication(FormDefinition form, int version) => new()
        {
            PublicationId = Guid.NewGuid(),
            FormId = form.Id,
            Version = version,
            PublishedAtUtc = DateTimeOffset.UtcNow,
            PublishedByUserId = Guid.NewGuid(),
            Definition = form
        };
    }

    private sealed class MemoryWorkflowStore : IWorkflowDefinitionStore
    {
        public WorkflowDefinition? SavedWorkflow { get; private set; }

        public ValueTask<WorkflowDefinition?> LoadAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<WorkflowDefinition?>(null);

        public ValueTask SaveAsync(
            WorkflowDefinition workflow,
            CancellationToken cancellationToken = default)
        {
            SavedWorkflow = workflow;
            return ValueTask.CompletedTask;
        }
    }
}
