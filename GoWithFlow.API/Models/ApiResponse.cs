using GoWithFlow.Application.Common;

namespace GoWithFlow.API.Models;

public sealed class ApiResponse<T> : GoWithFlow.Application.Common.ApiResponse<T>
{
	public static new ApiResponse<T> SuccessResult(T data, string message)
	{
		return new ApiResponse<T>
		{
			Success = true,
			Message = message,
			Data = data,
			Errors = null
		};
	}

	public static new ApiResponse<T> FailureResult(IEnumerable<string> errors, string message = "Operation failed")
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
