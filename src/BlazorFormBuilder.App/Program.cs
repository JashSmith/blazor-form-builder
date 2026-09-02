using BlazorFormBuilder.App;
using BlazorFormBuilder.Plugins.Standard;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddStandardFormFieldPlugins();

await builder.Build().RunAsync();
