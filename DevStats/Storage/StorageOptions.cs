namespace DevStats.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string DataDirectory { get; set; } = "App_Data";
}
