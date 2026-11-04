using Microsoft.AspNetCore.Diagnostics;

namespace DevStats.Api;

public static class ApiEndpointExtensions
{I 
    public static TBuilder WithoutStatusCodePages<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            if (context.HttpContext.Features.Get<IStatusCodePagesFeature>() is { } feature)
            {
                feature.Enabled = false;
            }

            return await next(context);
        });
}
