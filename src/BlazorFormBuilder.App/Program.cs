using BlazorFormBuilder.App;
using BlazorFormBuilder.App.Storage;
using BlazorFormBuilder.Core.Storage;
using BlazorFormBuilder.Plugins.Standard;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddStandardFormFieldPlugins();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<BrowserFormDefinitionStore>();
builder.Services.AddScoped<BrowserBuilderWorkspaceStore>();
builder.Services.AddScoped<ServerFormDefinitionStore>();
builder.Services.AddScoped<ServerBuilderWorkspaceStore>();
builder.Services.AddScoped<IFormDefinitionStore, ResilientFormDefinitionStore>();
builder.Services.AddScoped<IBuilderWorkspaceStore, ResilientBuilderWorkspaceStore>();

await builder.Build().RunAsync();
