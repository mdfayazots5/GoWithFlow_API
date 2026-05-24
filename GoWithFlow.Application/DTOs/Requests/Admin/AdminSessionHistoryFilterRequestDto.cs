namespace GoWithFlow.Application.DTOs.Requests.Admin;

public sealed class AdminSessionHistoryFilterRequestDto
{
    public string? SearchTerm   { get; set; }   // matches session name or host name
    public string?  Status       { get; set; }   // COMPLETED | ABANDONED | IN_PROGRESS | (empty = all)
    public DateTime? FromDate    { get; set; }
    public DateTime? ToDate      { get; set; }
    public int       PageNumber  { get; set; } = 1;
    public int       PageSize    { get; set; } = 20;
}
