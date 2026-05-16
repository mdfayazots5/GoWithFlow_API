namespace GoWithFlow.Application.Common;

public class ApiResponse<T>
{
	public bool Success { get; init; }

	public string Message { get; init; } = string.Empty;

	public T? Data { get; init; }

	public List<string>? Errors { get; init; }

	public static ApiResponse<T> SuccessResult(T data, string message)
	{
		return new ApiResponse<T>
		{
			Success = true,
			Message = message,
			Data = data,
			Errors = null
		};
	}

	public static ApiResponse<T> FailureResult(IEnumerable<string> errors, string message = "Operation failed")
	{
		return new ApiResponse<T>
		{
			Success = false,
			Message = message,
			Data = default,
			Errors = errors.ToList()
		};
	}
}
