using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RAGPrompterWebApp.Data.Interfaces;
using RAGPrompterWebApp.Data.Services;
using RAGPrompterWebApp;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Remove the default HttpClient registration and add one for the API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddScoped<IProjectService, ProjectService>();

await builder.Build().RunAsync();