using Asterism.Server;
using Asterism.Server.Endpoints;
using Asterism.Server.Services;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ManifestStore>();

var app = builder.Build();

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".exe"] = "application/octet-stream";
contentTypeProvider.Mappings[".msi"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    ServeUnknownFileTypes = false
});

app.UseMiddleware<AdminAuthMiddleware>();
app.MapAdminToolsEndpoints();

app.Run();
