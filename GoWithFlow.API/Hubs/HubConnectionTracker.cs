using System.Collections.Concurrent;

namespace GoWithFlow.API.Hubs;

public interface IHubConnectionTracker
{
	void TrackConnection(string connectionId, HubConnectionMetadata metadata);

	bool TryGetConnection(string connectionId, out HubConnectionMetadata? metadata);

	bool TryRemoveConnection(string connectionId, out HubConnectionMetadata? metadata);
}

public sealed record HubConnectionMetadata(
	long SessionId,
	long UserId,
	string GroupName,
	string? FullName = null,
	byte? SlotIndex = null);

public sealed class HubConnectionTracker : IHubConnectionTracker
{
	private readonly ConcurrentDictionary<string, HubConnectionMetadata> _connections = new();

	public void TrackConnection(string connectionId, HubConnectionMetadata metadata)
	{
		_connections[connectionId] = metadata;
	}

	public bool TryGetConnection(string connectionId, out HubConnectionMetadata? metadata)
	{
		return _connections.TryGetValue(connectionId, out metadata);
	}

	public bool TryRemoveConnection(string connectionId, out HubConnectionMetadata? metadata)
	{
		return _connections.TryRemove(connectionId, out metadata);
	}
}
