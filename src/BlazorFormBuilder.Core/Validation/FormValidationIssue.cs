namespace BlazorFormBuilder.Core.Validation;

public sealed record FormValidationIssue(Guid? FieldId, string Code, string Message);
