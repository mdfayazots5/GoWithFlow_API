using System.Text.Json;
using FluentValidation;
using GoWithFlow.Application.Common;
using GoWithFlow.Domain.Exceptions;

namespace GoWithFlow.API.Middleware;

public sealed class ExceptionMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<ExceptionMiddleware> _logger;

	public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (Exception exception)
		{
			_logger.LogError(
				exception,
				"Unhandled exception occurred while processing request {RequestPath} for UserId {UserId}.",
				context.Request.Path,
				context.User.FindFirst("UserId")?.Value ?? "anonymous");
			await HandleExceptionAsync(context, exception);
		}
	}

	private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
	{
		int statusCode;
		ApiResponse<object> response;

		switch (exception)
		{
			case ValidationException validationException:
				statusCode = StatusCodes.Status400BadRequest;
				response   = ApiResponse<object>.FailureResult(validationException.Errors.Select(e => e.ErrorMessage), "Validation failed");
				break;

			case UnauthorizedAccessException:
				statusCode = StatusCodes.Status401Unauthorized;
				response   = ApiResponse<object>.FailureResult(new[] { exception.Message }, "Unauthorized");
				break;

			case KeyNotFoundException:
				statusCode = StatusCodes.Status404NotFound;
				response   = ApiResponse<object>.FailureResult(new[] { "Resource not found." }, "Resource not found");
				break;

			case InvalidOperationException:
				statusCode = StatusCodes.Status422UnprocessableEntity;
				response   = ApiResponse<object>.FailureResult(new[] { exception.Message }, "Unprocessable entity");
				break;

			case StorageException storageEx:
				statusCode = StatusCodes.Status502BadGateway;
				response   = ApiResponse<object>.FailureResult(
					new[] { "File storage operation failed. Please try again." },
					"Storage error");
				// Bucket and ObjectKey already captured in the exception — logged by InvokeAsync above
				_ = storageEx; // suppress unused-variable warning
				break;

			default:
				statusCode = StatusCodes.Status500InternalServerError;
				response   = ApiResponse<object>.FailureResult(new[] { "An internal error occurred." }, "Internal server error");
				break;
		}

		context.Response.ContentType = "application/json";
		context.Response.StatusCode  = statusCode;

		await context.Response.WriteAsync(JsonSerializer.Serialize(response));
	}
}
