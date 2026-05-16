namespace GoWithFlow.Application.DTOs.Requests.User;

public sealed class UpdateAttemptRequestDto
{
	public long RepracticeUtteranceId { get; set; }

	public decimal Score { get; set; }
}
