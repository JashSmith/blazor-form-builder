using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Services;

namespace BlazorFormBuilder.Core.Tests;

public sealed class FormDefinitionServiceTests
{
    [Fact]
    public void AddFieldAppendsFieldAndSetsOrder()
    {
        var form = FormDefinitionService.Create("Registration");
        var field = Field("email");

        FormDefinitionService.AddField(form, field);

        Assert.Same(field, Assert.Single(form.Fields));
        Assert.Equal(0, field.Order);
    }

    [Fact]
    public void AddFieldRejectsDuplicateKeyIgnoringCase()
    {
        var form = FormDefinitionService.Create("Registration");
        FormDefinitionService.AddField(form, Field("email"));

        var action = () => FormDefinitionService.AddField(form, Field("EMAIL"));

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void MoveFieldReordersAndNormalizesOrder()
    {
        var form = FormDefinitionService.Create("Registration");
        var first = Field("first");
        var second = Field("second");
        FormDefinitionService.AddField(form, first);
        FormDefinitionService.AddField(form, second);

        var moved = FormDefinitionService.MoveField(form, second.Id, -1);

        Assert.True(moved);
        Assert.Equal(second.Id, form.Fields[0].Id);
        Assert.Collection(
            form.Fields,
            field => Assert.Equal(0, field.Order),
            field => Assert.Equal(1, field.Order));
    }

    [Fact]
    public void RemoveFieldNormalizesRemainingOrder()
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
    public void CreateUniqueKeyIncrementsExistingSuffix()
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
