using System.Text.Json.Serialization;

namespace DevStats.Websites.Monitoring;

[JsonConverter(typeof(JsonStringEnumConverter<WebsiteState>))]
public enum WebsiteState
{
    /// <summary>Enabled but not checked yet.</summary>
    Pending,
    Up,
    Down,
    Disabled,
}

/// <summary>Result of the most recent check of one website.</summary>
public sealed record WebsiteCheckResult(
    Guid WebsiteId,
    string Url,
    bool IsUp,
    int? StatusCode,
    long? ResponseTimeMs,
    string? Error,
    DateTimeOffset CheckedAt);

/// <summary>A website joined with its latest check result: what the dashboard and API show.</summary>
public sealed record WebsiteStatus(
    Guid Id,
    string Name,
    string Url,
    string? Environment,
    WebsiteState State,
    int? StatusCode,
    long? ResponseTimeMs,
    string? Error,
    DateTimeOffset? CheckedAt);
