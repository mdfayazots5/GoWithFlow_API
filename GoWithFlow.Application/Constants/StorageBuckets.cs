using GoWithFlow.Application.Settings;

namespace GoWithFlow.Application.Constants;

public enum StorageBucket
{
    Audio,
    Avatars,
    Scripts,
    Exports
}

public static class BucketResolver
{
    public static string Resolve(StorageBucket bucket, CloudflareR2Settings settings)
        => bucket switch
        {
            StorageBucket.Audio   => settings.Buckets.Audio,
            StorageBucket.Avatars => settings.Buckets.Avatars,
            StorageBucket.Scripts => settings.Buckets.Scripts,
            StorageBucket.Exports => settings.Buckets.Exports,
            _ => throw new ArgumentOutOfRangeException(nameof(bucket))
        };
}
