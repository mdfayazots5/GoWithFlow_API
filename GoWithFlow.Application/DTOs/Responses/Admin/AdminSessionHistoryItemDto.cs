namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class AdminSessionHistoryItemDto
{
    public long      SessionId    { get; set; }
    public string    SessionName  { get; set; } = string.Empty;
    public string    JoinCode     { get; set; } = string.Empty;
    public string    HostName     { get; set; } = string.Empty;
    public int       MemberCount  { get; set; }
    public string    Status       { get; set; } = string.Empty;
    public DateTime  SessionDate  { get; set; }
    public int       DurationMin  { get; set; }
    public decimal   AvgFluency   { get; set; }
    public int       MistakeCount { get; set; }
}
