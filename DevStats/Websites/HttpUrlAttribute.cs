using System.ComponentModel.DataAnnotations;

namespace DevStats.Websites;

/// <summary>Requires an absolute http:// or https:// URL. Empty values are left to [Required].</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class HttpUrlAttribute() : ValidationAttribute("{0} must be an absolute http:// or https:// address.")
{
    public override bool IsValid(object? value) =>
        value is not string { Length: > 0 } text ||
        (Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
}
