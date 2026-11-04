using DevStats.Api;

namespace DevStats.Websites.Monitoring;

/// <summary>Read-only status API: /api/status/...</summary>
public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/status")
            .WithTags("Status")
            .WithoutStatusCodePages();

        group.MapGet("/websites", async (WebsiteStatusService statuses, CancellationToken ct) =>
            TypedResults.Ok(await statuses.GetAllAsync(ct)));

        return app;
    }
}
