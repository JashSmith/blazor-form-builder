using BlazorFormBuilder.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorFormBuilder.Plugins.Standard;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStandardFormFieldPlugins(this IServiceCollection services)
    {
        Add(services, "text", "Text input", "Text field", "Enter a value");
        Add(services, "email", "Email", "Email address", "name@example.com");
        Add(services, "number", "Number", "Number", "Enter a number");
        Add(services, "date", "Date", "Date");
        Add(services, "textarea", "Long text", "Description", "Enter details");
        Add(services, "checkbox", "Checkbox", "Confirmation", "I confirm this information");
        return services;
    }

    private static void Add(
        IServiceCollection services,
        string type,
        string displayName,
        string label,
        string? placeholder = null) =>
        services.AddSingleton<IFormFieldPlugin>(
            new StandardFieldPlugin(type, displayName, label, placeholder));
}
