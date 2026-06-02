namespace GoWithFlow.Domain.Exceptions;

public sealed class StorageException : Exception
{
    public string Bucket    { get; }
    public string ObjectKey { get; }

    public StorageException(string bucket, string objectKey, string message, Exception? inner = null)
        : base(message, inner)
    {
        Bucket    = bucket;
        ObjectKey = objectKey;
    }
}
