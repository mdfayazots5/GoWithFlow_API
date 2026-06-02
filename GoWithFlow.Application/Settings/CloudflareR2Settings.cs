namespace GoWithFlow.Application.Settings;

public sealed class CloudflareR2Settings
{
    public string AccountEndpoint  { get; set; } = string.Empty;
    public string AccessKeyId      { get; set; } = string.Empty;
    public string SecretAccessKey  { get; set; } = string.Empty;
    public BucketSettings Buckets  { get; set; } = new();
    public PresignedExpirySettings PresignedUrlExpiryMinutes { get; set; } = new();
}

public sealed class BucketSettings
{
    public string Audio   { get; set; } = "gwf-audio";
    public string Avatars { get; set; } = "gwf-avatars";
    public string Scripts { get; set; } = "gwf-scripts";
    public string Exports { get; set; } = "gwf-exports";
}

public sealed class PresignedExpirySettings
{
    public int Audio   { get; set; } = 120;
    public int Avatars { get; set; } = 1440;
    public int Scripts { get; set; } = 60;
    public int Exports { get; set; } = 30;
}
