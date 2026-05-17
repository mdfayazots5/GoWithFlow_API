using GoWithFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Data;

public sealed class GoWithFlowDbContext : DbContext
{
	public GoWithFlowDbContext(DbContextOptions<GoWithFlowDbContext> options)
		: base(options)
	{
	}

	public DbSet<User> Users => Set<User>();

public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

	public DbSet<AdminNote> AdminNotes => Set<AdminNote>();

	public DbSet<DashboardMetric> DashboardMetrics => Set<DashboardMetric>();

	public DbSet<Script> Scripts => Set<Script>();

	public DbSet<Utterance> Utterances => Set<Utterance>();

	public DbSet<ScriptVersion> ScriptVersions => Set<ScriptVersion>();

	public DbSet<Session> Sessions => Set<Session>();

	public DbSet<SessionMember> SessionMembers => Set<SessionMember>();

	public DbSet<TurnState> TurnStates => Set<TurnState>();

	public DbSet<VoiceAnalysis> VoiceAnalyses => Set<VoiceAnalysis>();

	public DbSet<ListenerFeedback> ListenerFeedbacks => Set<ListenerFeedback>();

	public DbSet<Mistake> Mistakes => Set<Mistake>();

	public DbSet<RepracticeSession> RepracticeSessions => Set<RepracticeSession>();

	public DbSet<RepracticeUtterance> RepracticeUtterances => Set<RepracticeUtterance>();

	public DbSet<UserStreak> UserStreaks => Set<UserStreak>();

	public DbSet<UserBadge> UserBadges => Set<UserBadge>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(GoWithFlowDbContext).Assembly);

		base.OnModelCreating(modelBuilder);
	}

	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		var entries = ChangeTracker
			.Entries<BaseAuditEntity>()
			.Where(entry => entry.State is EntityState.Added or EntityState.Modified);

		foreach (var entry in entries)
		{
			if (entry.State == EntityState.Added)
			{
				entry.Entity.DateCreated = DateTime.UtcNow;

				if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy))
				{
					entry.Entity.CreatedBy = "Admin";
				}

				if (string.IsNullOrWhiteSpace(entry.Entity.IPAddress))
				{
					entry.Entity.IPAddress = "127.0.0.1";
				}
			}

			if (entry.State == EntityState.Modified)
			{
				entry.Entity.LastUpdated = DateTime.UtcNow;
			}
		}

		return await base.SaveChangesAsync(cancellationToken);
	}
}
