using System.Diagnostics;

namespace DevStats.Websites.Monitoring;

/// <summary>Makes one HTTP request to a website and decides whether it is up.</summary>
public sealed class WebsiteChecker(IHttpClientFactory httpClientFactory)
{
    public const string HttpClientName = "WebsiteChecker";

    public async Task<WebsiteCheckResult> CheckAsync(Website website, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(website.TimeoutSeconds));

        var http = httpClientFactory.CreateClient(HttpClientName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, website.Url);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            stopwatch.Stop();

            var statusCode = (int)response.StatusCode;
            var isUp = website.ExpectedStatusCode is { } expected
                ? statusCode == expected
                : response.IsSuccessStatusCode;

            var error = isUp
                ? null
                : website.ExpectedStatusCode is { } want
                    ? $"Expected {want}, got {statusCode} {response.ReasonPhrase}"
                    : $"HTTP {statusCode} {response.ReasonPhrase}";

            return Result(isUp, statusCode, stopwatch.ElapsedMilliseconds, error);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Result(false, null, null, $"Timed out after {website.TimeoutSeconds}s");
        }
        catch (HttpRequestException ex)
        {
            return Result(false, null, null, ex.Message);
        }

        WebsiteCheckResult Result(bool isUp, int? statusCode, long? elapsedMs, string? error) =>
            new(website.Id, website.Url, isUp, statusCode, elapsedMs, error, DateTimeOffset.UtcNow);
    }
}
