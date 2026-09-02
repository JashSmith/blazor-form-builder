using BlazorFormBuilder.Core.Models;
using BlazorFormBuilder.Core.Services;
using BlazorFormBuilder.Core.Validation;

namespace BlazorFormBuilder.Core.Tests;

public sealed class FormDefinitionValidatorTests
{
    [Fact]
    public void EmptyFormReturnsNameAndFieldIssues()
    {
        var form = FormDefinitionService.Create(" ");
        form.Name = " ";

        var issues = FormDefinitionValidator.Validate(form);

        Assert.Contains(issues, issue => issue.Code == "form.name.required");
        Assert.Contains(issues, issue => issue.Code == "form.fields.required");
    }

    [Fact]
    public void InvalidAndDuplicateKeysAreReported()
    {
        var form = FormDefinitionService.Create("Registration");
        FormDefinitionService.AddField(form, Field("first name"));
        FormDefinitionService.AddField(form, Field("duplicate"));
        form.Fields.Add(Field("DUPLICATE"));

        var issues = FormDefinitionValidator.Validate(form);

        Assert.Contains(issues, issue => issue.Code == "field.key.invalid");
        Assert.Equal(2, issues.Count(issue => issue.Code == "field.key.duplicate"));
    }

    [Fact]
    public void ValidDefinitionReturnsNoIssues()
    {
        var form = FormDefinitionService.Create("Registration");
        FormDefinitionService.AddField(form, Field("first_name"));

        Assert.Empty(FormDefinitionValidator.Validate(form));
    }

    private static FormFieldDefinition Field(string key) => new()
    {
        Id = Guid.NewGuid(),
        Type = "text",
        Key = key,
        Label = "Field"
    };
}
