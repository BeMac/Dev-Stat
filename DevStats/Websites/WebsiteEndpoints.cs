using System.ComponentModel.DataAnnotations;
using DevStats.Api;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DevStats.Websites;

/// <summary>REST API for managing monitored websites: /api/websites</summary>
public static class WebsiteEndpoints
{
    public static IEndpointRouteBuilder MapWebsiteApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/websites")
            .WithTags("Websites")
            .WithoutStatusCodePages();

        group.MapGet("/", async (IWebsiteStore store, CancellationToken ct) =>
            TypedResults.Ok(await store.GetAllAsync(ct)));

        group.MapGet("/{id:guid}", async Task<Results<Ok<Website>, NotFound>> (
            Guid id, IWebsiteStore store, CancellationToken ct) =>
            await store.GetAsync(id, ct) is { } website
                ? TypedResults.Ok(website)
                : TypedResults.NotFound());

        group.MapPost("/", async Task<Results<Created<Website>, ValidationProblem>> (
            Website website, IWebsiteStore store, CancellationToken ct) =>
        {
            if (!TryValidate(website, out var errors)) return TypedResults.ValidationProblem(errors);

            var created = await store.AddAsync(website, ct);
            return TypedResults.Created($"/api/websites/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async Task<Results<Ok<Website>, NotFound, ValidationProblem>> (
            Guid id, Website website, IWebsiteStore store, CancellationToken ct) =>
        {
            website.Id = id;
            if (!TryValidate(website, out var errors)) return TypedResults.ValidationProblem(errors);

            return await store.UpdateAsync(website, ct)
                ? TypedResults.Ok(website)
                : TypedResults.NotFound();
        });

        group.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id, IWebsiteStore store, CancellationToken ct) =>
            await store.DeleteAsync(id, ct)
                ? TypedResults.NoContent()
                : TypedResults.NotFound());

        return app;
    }

    private static bool TryValidate(object model, out Dictionary<string, string[]> errors)
    {
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(""), (r, member) => (member, message: r.ErrorMessage ?? "Invalid value."))
            .GroupBy(e => e.member, e => e.message)
            .ToDictionary(g => g.Key, g => g.ToArray());

        return isValid;
    }
}
