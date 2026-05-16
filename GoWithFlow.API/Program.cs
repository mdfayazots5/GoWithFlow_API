using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using GoWithFlow.API.Authorization;
using GoWithFlow.API.Constants;
using GoWithFlow.API.Extensions;
using GoWithFlow.API.Hubs;
using GoWithFlow.API.Middleware;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Mappings;
using GoWithFlow.Application.Services;
using GoWithFlow.Application.Settings;
using GoWithFlow.Application.Validators;
using GoWithFlow.Infrastructure.Data;
using GoWithFlow.Infrastructure.ExternalServices;
using GoWithFlow.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
	var minimumLevel = context.HostingEnvironment.IsDevelopment()
		? LogEventLevel.Debug
		: LogEventLevel.Warning;

	configuration
		.ReadFrom.Configuration(context.Configuration)
		.ReadFrom.Services(services)
		.MinimumLevel.Is(minimumLevel)
		.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
		.MinimumLevel.Override("System", LogEventLevel.Warning)
		.Enrich.FromLogContext()
		.Enrich.WithMachineName()
		.Enrich.WithThreadId()
		.WriteTo.Console(restrictedToMinimumLevel: minimumLevel)
		.WriteTo.File(
			path: "logs/gwf-.txt",
			rollingInterval: RollingInterval.Day,
			restrictedToMinimumLevel: minimumLevel,
			shared: true);
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<OtpSettings>(builder.Configuration.GetSection("OtpSettings"));
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorage"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<GoWithFlowDbContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services
	.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	});

builder.Services.AddSignalR(options =>
{
	options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddApiVersioning(options =>
{
	options.DefaultApiVersion = new ApiVersion(1, 0);
	options.AssumeDefaultVersionWhenUnspecified = true;
	options.ReportApiVersions = true;
	options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddVersionedApiExplorer(options =>
{
	options.GroupNameFormat = "'v'VVV";
	options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Go With Flow API",
		Version = "v1"
	});

	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Enter JWT Bearer token."
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			Array.Empty<string>()
		}
	});
});

builder.Services.AddAutoMapper(typeof(AuthMappingProfile).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
	?? throw new InvalidOperationException("JwtSettings configuration is missing.");

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateIssuerSigningKey = true,
			ValidateLifetime = true,
			ValidIssuer = jwtSettings.Issuer,
			ValidAudience = jwtSettings.Audience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
			ClockSkew = TimeSpan.Zero
		};

		options.Events = new JwtBearerEvents
		{
			OnMessageReceived = context =>
			{
				var accessToken = context.Request.Query["access_token"];
				var requestPath = context.HttpContext.Request.Path;

				if (string.IsNullOrWhiteSpace(accessToken) == false &&
					(requestPath.StartsWithSegments(ApiRoutes.Hub.Session) ||
					requestPath.StartsWithSegments(ApiRoutes.Hub.LiveSession)))
				{
					context.Token = accessToken;
				}

				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
	{
		policy.RequireRole("ADMIN");
	});

	options.AddPolicy(AuthorizationPolicies.UserOrAdmin, policy =>
	{
		policy.RequireRole("USER", "ADMIN");
	});

	options.AddPolicy(AuthorizationPolicies.ActiveUser, policy =>
	{
		policy.RequireAuthenticatedUser();
		policy.AddRequirements(new ActiveUserRequirement());
	});
});

builder.Services.AddCors(options =>
{
	options.AddPolicy(CorsPolicyNames.Development, policy =>
	{
		policy
			.SetIsOriginAllowed(_ => true)
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});

	var allowedOrigins = builder.Configuration.GetSection("CorsSettings").GetSection("AllowedOrigins").Get<string[]>()
		?? builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
		?? Array.Empty<string>();

	options.AddPolicy(CorsPolicyNames.Production, policy =>
	{
		if (allowedOrigins.Length > 0)
		{
			policy
				.WithOrigins(allowedOrigins)
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials();
		}
	});
});

builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
	{
		var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

		return RateLimitPartition.GetFixedWindowLimiter(
			partitionKey,
			_ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 100,
				Window = TimeSpan.FromMinutes(1),
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit = 0
			});
	});

	options.AddPolicy(RateLimitPolicyNames.AuthEndpoints, context =>
	{
		var partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

		return RateLimitPartition.GetFixedWindowLimiter(
			partitionKey,
			_ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 10,
				Window = TimeSpan.FromMinutes(1),
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit = 0
			});
	});
});

builder.Services
	.AddHealthChecks()
	.AddDbContextCheck<GoWithFlowDbContext>("sqlserver", tags: new[] { "db" });

builder.Services.AddSingleton<IUserIdProvider, JwtUserIdProvider>();
builder.Services.AddSingleton<IHubConnectionTracker, HubConnectionTracker>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveUserRequirementHandler>();

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IScriptRepository, ScriptRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ILiveSessionRepository, LiveSessionRepository>();
builder.Services.AddScoped<IMistakeRepository, MistakeRepository>();
builder.Services.AddScoped<IRepracticeRepository, RepracticeRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IScriptService, ScriptService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ILiveSessionService, LiveSessionService>();
builder.Services.AddScoped<IMistakeService, MistakeService>();
builder.Services.AddScoped<IRepracticeService, RepracticeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserDashboardService, UserDashboardService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddTransient<IOtpService, OtpService>();
builder.Services.AddScoped<IExcelParserService, ExcelParserService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
	options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
	{
		diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
		diagnosticContext.Set("UserId", httpContext.User.FindFirst("UserId")?.Value ?? "anonymous");

		if (httpContext.Request.RouteValues.TryGetValue("sessionId", out var sessionIdValue) && sessionIdValue is not null)
		{
			diagnosticContext.Set("SessionId", sessionIdValue.ToString() ?? string.Empty);
		}
	};
});

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors(app.Environment.IsDevelopment() ? CorsPolicyNames.Development : CorsPolicyNames.Production);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks(ApiRoutes.Health.Overall, new HealthCheckOptions
{
	Predicate = _ => true
}).AllowAnonymous();

app.MapHealthChecks(ApiRoutes.Health.Database, new HealthCheckOptions
{
	Predicate = check => check.Tags.Contains("db")
}).AllowAnonymous();

app.MapHealthChecks(ApiRoutes.Health.Detailed, new HealthCheckOptions
{
	Predicate = _ => true,
	ResponseWriter = HealthCheckResponseWriter.WriteAsync
}).AllowAnonymous();

app.MapControllers();
app.MapHub<SessionHub>(ApiRoutes.Hub.Session);
app.MapHub<LiveSessionHub>(ApiRoutes.Hub.LiveSession);

app.Run();
