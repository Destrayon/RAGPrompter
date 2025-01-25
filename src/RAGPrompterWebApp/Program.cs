using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RAGPrompterWebApp;
using RAGPrompterWebApp.Data.Interfaces;
using RAGPrompterWebApp.Data.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IFileService, FileService>();

await builder.Build().RunAsync();
