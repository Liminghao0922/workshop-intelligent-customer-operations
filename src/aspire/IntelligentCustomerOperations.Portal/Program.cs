using IntelligentCustomerOperations.Portal.Components;
using IntelligentCustomerOperations.Portal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton(
    _ =>
    {
        var baseUrl =
            Environment.GetEnvironmentVariable("BACKEND_API_BASE_URL")
            ?? "http://localhost:8081";
        return new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/')) };
    }
);
builder.Services.AddSingleton<ApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

