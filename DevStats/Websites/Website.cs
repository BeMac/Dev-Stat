using System.ComponentModel.DataAnnotations;

namespace DevStats.Websites;

public sealed class Website
{
    public Guid Id { get; set; }
    [Required, StringLength(100)]
    public string Name { get; set; } = "";
    [Required, StringLength(2048), HttpUrl]
    public string Url { get; set; } = "";
    [StringLength(50)]
    public string? Environment { get; set; }
    public bool Enabled { get; set; } = true;
    [Range(10, 86400, ErrorMessage = "Check interval must be between 10 and 86400 seconds.")]
    public int CheckIntervalSeconds { get; set; } = 60;
    [Range(1, 120, ErrorMessage = "Timeout must be between 1 and 120 seconds.")]
    public int TimeoutSeconds { get; set; } = 10;
    [Range(100, 599, ErrorMessage = "Expected status code must be between 100 and 599.")]
    public int? ExpectedStatusCode { get; set; }
    public Website Clone() => (Website)MemberwiseClone();
}
