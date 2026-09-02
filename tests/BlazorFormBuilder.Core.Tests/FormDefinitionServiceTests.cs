using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Services;

namespace BlazorFormBuilder.Core.Tests;

public sealed class FormDefinitionServiceTests
{
    [Fact]
    public void AddField_AppendsFieldAndSetsOrder()
    {
        var form = FormDefinitionService.Create("Registration");
        var field = Field("email");

        FormDefinitionService.AddField(form, field);

        Assert.Same(field, Assert.Single(form.Fields));
        Assert.Equal(0, field.Order);
    }

    [Fact]
    public void AddField_RejectsDuplicateKeyIgnoringCase()
    {
        var form = FormDefinitionService.Create("Registration");
        FormDefinitionService.AddField(form, Field("email"));

        var action = () => FormDefinitionService.AddField(form, Field("EMAIL"));

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void MoveField_ReordersAndNormalizesOrder()
    {
        var form = FormDefinitionService.Create("Registration");
        var first = Field("first");
        var second = Field("second");
        FormDefinitionService.AddField(form, first);
        FormDefinitionService.AddField(form, second);

        var moved = FormDefinitionService.MoveField(form, second.Id, -1);

        Assert.True(moved);
        Assert.Equal(second.Id, form.Fields[0].Id);
        Assert.Equal(new[] { 0, 1 }, form.Fields.Select(field => field.Order));
    }

    [Fact]
    public void RemoveField_NormalizesRemainingOrder()
    {
        var form = FormDefinitionService.Create("Registration");
        var first = Field("first");
        var second = Field("second");
        FormDefinitionService.AddField(form, first);
        FormDefinitionService.AddField(form, second);

        FormDefinitionService.RemoveField(form, first.Id);

        Assert.Equal(0, Assert.Single(form.Fields).Order);
    }

    [Fact]
    public void CreateUniqueKey_IncrementsExistingSuffix()
    {
        var form = FormDefinitionService.Create("Registration");
        FormDefinitionService.AddField(form, Field("text"));
        FormDefinitionService.AddField(form, Field("text2"));

        Assert.Equal("text3", FormDefinitionService.CreateUniqueKey(form, "text"));
    }

    private static FormFieldDefinition Field(string key) => new()
    {
        Id = Guid.NewGuid(),
        Type = "text",
        Key = key,
        Label = key
    };
}
