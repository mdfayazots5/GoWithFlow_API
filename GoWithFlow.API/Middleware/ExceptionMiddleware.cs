using System.Text.Json;
using FluentValidation;
using GoWithFlow.Application.Common;

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
		var (statusCode, response) = exception switch
		{
			ValidationException validationException => (
				StatusCodes.Status400BadRequest,
				ApiResponse<object>.FailureResult(validationException.Errors.Select(error => error.ErrorMessage), "Validation failed")),
			UnauthorizedAccessException => (
				StatusCodes.Status401Unauthorized,
				ApiResponse<object>.FailureResult(new[] { exception.Message }, "Unauthorized")),
			KeyNotFoundException => (
				StatusCodes.Status404NotFound,
				ApiResponse<object>.FailureResult(new[] { "Resource not found." }, "Resource not found")),
			InvalidOperationException => (
				StatusCodes.Status422UnprocessableEntity,
				ApiResponse<object>.FailureResult(new[] { exception.Message }, "Unprocessable entity")),
			_ => (
				StatusCodes.Status500InternalServerError,
				ApiResponse<object>.FailureResult(new[] { "An internal error occurred." }, "Internal server error"))
		};

		context.Response.ContentType = "application/json";
		context.Response.StatusCode = statusCode;

		await context.Response.WriteAsync(JsonSerializer.Serialize(response));
	}
}
