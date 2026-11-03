using DevHealthChecker.Components;
using DevHealthChecker.Storage;
using DevHealthChecker.Websites;
using DevHealthChecker.Websites.Monitoring;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<IWebsiteStore, JsonWebsiteStore>();

// Website up/down monitoring
builder.Services.AddHttpClient(WebsiteChecker.HttpClientName, client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan; // each check applies its own per-site timeout
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DevHealthChecker/1.0");
});
builder.Services.AddSingleton<WebsiteChecker>();
builder.Services.AddSingleton<WebsiteCheckResultStore>();
builder.Services.AddSingleton<WebsiteStatusService>();
builder.Services.AddHostedService<WebsiteMonitorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapWebsiteApi();
app.MapStatusApi();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();