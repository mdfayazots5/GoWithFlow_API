using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GoWithFlow.API.Extensions;

public static class HealthCheckResponseWriter
{
	public static Task WriteAsync(HttpContext context, HealthReport report)
	{
		context.Response.ContentType = "application/json";

		var payload = new
		{
			status = report.Status.ToString(),
			totalDuration = report.TotalDuration.TotalMilliseconds,
			entries = report.Entries.ToDictionary(
				entry => entry.Key,
				entry => new
				{
					status = entry.Value.Status.ToString(),
					description = entry.Value.Description,
					duration = entry.Value.Duration.TotalMilliseconds,
					exception = entry.Value.Exception?.Message,
					tags = entry.Value.Tags.ToArray()
				})
		};

		return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
	}
}
