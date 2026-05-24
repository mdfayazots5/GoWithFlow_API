namespace GoWithFlow.API.Constants;

public static class ApiRoutes
{
	public const string VersionPrefix = "api";

	public static class Auth
	{
		public const string Base = VersionPrefix + "/auth";
		public const string Login = "login";
		public const string Register = "register";
		public const string RefreshToken = "refresh-token";
		public const string Logout = "logout";
	}

	public static class User
	{
		public const string Base = VersionPrefix + "/users";
		public const string Profile = "profile";
		public const string Avatar = "profile/avatar";
		public const string SessionDetail = "sessions/{sessionId:long}/detail";
		public const string Progress = "progress";
		public const string Streak = "streak";
		public const string Badges = "badges";
	}

	public static class Dashboard
	{
		public const string Base = VersionPrefix + "/dashboard";
	}

	public static class Admin
	{
		public const string Base = VersionPrefix + "/admin";
		public const string Dashboard = "dashboard";
		public const string Users = "users";
		public const string UserDetail = "users/{userId:long}";
		public const string UserStatus = "users/status";
		public const string UserNotes = "users/notes";
		public const string UserNotesByUser = "users/{userId:long}/notes";
		public const string Reports        = "reports";
		public const string UserReport     = "reports/users/{userId:long}";
		public const string ExportReports  = "reports/export";
		public const string SessionHistory = "sessions/history";
	}

	public static class Script
	{
		public const string Base = VersionPrefix + "/scripts";
		public const string Validate = "validate";
		public const string Upload = "upload";
		public const string Detail = "{scriptId:long}";
		public const string Status = "status";
		public const string Versions = "{scriptId:long}/versions";
		public const string SampleTemplate = "sample-template";
		public const string Download = "{scriptId:long}/download";
	}

	public static class Session
	{
		public const string Base = VersionPrefix + "/sessions";
		public const string ValidateJoinCode = "validate/{joinCode}";
		public const string Join = "join";
		public const string Lobby = "lobby/{sessionId:long}";
		public const string Ready = "ready";
		public const string Start = "{sessionId:long}/start";
		public const string End = "{sessionId:long}/end";
		public const string History = "history";
		public const string Leave = "{sessionId:long}/leave";
		public const string CompleteAbsolute = "~/api/sessions/{sessionId:long}/complete";
	}

	public static class LiveSession
	{
		public const string Base = VersionPrefix + "/turns";
		public const string Current = "{sessionId:long}/current";
		public const string Shift = "{sessionId:long}/shift";
		public const string VoiceAnalysis = "{sessionId:long}/voice-analysis";
		public const string ListenerFeedback = "{sessionId:long}/listener-feedback";
		public const string ReRead = "{sessionId:long}/re-read";
	}

	public static class Mistake
	{
		public const string Base = VersionPrefix + "/mistakes";
		public const string Summary = "summary";
		public const string GrammarProgress = "grammar-progress";
	}

	public static class Repractice
	{
		public const string Base = VersionPrefix + "/repractice";
		public const string Generate = "generate";
		public const string Detail = "{repracticeSessionId:long}";
		public const string History = "history";
		public const string Attempt = "attempt";
		public const string Complete = "{repracticeSessionId:long}/complete";
	}

	public static class Hub
	{
		public const string Session = "/hubs/session";
		public const string LiveSession = "/hubs/live-session";
	}

	public static class Health
	{
		public const string Overall = "/api/health";
		public const string Database = "/api/health/db";
		public const string Detailed = "/api/health/detailed";
	}
}
