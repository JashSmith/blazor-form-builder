using BlazorFormBuilder.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorFormBuilder.Plugins.Standard;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStandardFormFieldPlugins(this IServiceCollection services)
    {
        services.AddSingleton<IFormFieldPlugin, TextFieldPlugin>();
        return services;
    }
}
