namespace GoWithFlow.Application.Settings;

public sealed class FileStorageSettings
{
	public string AvatarPath { get; set; } = "wwwroot/avatars/";

	public int MaxFileSizeMB { get; set; } = 2;
}
