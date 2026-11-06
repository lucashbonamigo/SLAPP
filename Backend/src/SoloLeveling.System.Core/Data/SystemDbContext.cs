using Microsoft.EntityFrameworkCore;
using SoloLeveling.System.Core.Domain;

namespace SoloLeveling.System.Core.Data;

/// <summary>
/// Contexto EF Core mapeado para o banco MySQL existente (database-first, sem migrations).
/// Os nomes de tabela/coluna seguem exatamente o script do banco.
/// </summary>
public class SystemDbContext : DbContext
{
    public SystemDbContext(DbContextOptions<SystemDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Quest> Quests => Set<Quest>();
    public DbSet<Proof> Proofs => Set<Proof>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Punishment> Punishments => Set<Punishment>();
    public DbSet<QuestMetric> QuestMetrics => Set<QuestMetric>();
    public DbSet<PersonalityTrait> PersonalityTraits => Set<PersonalityTrait>();
    public DbSet<PersonalityTraitHistory> PersonalityTraitHistory => Set<PersonalityTraitHistory>();
    public DbSet<DailyReflection> DailyReflections => Set<DailyReflection>();
    public DbSet<DailyBriefing> DailyBriefings => Set<DailyBriefing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(e => e.Level).HasColumnName("level").HasDefaultValue(1);
            entity.Property(e => e.CurrentXp).HasColumnName("current_xp").HasDefaultValue(0);
            entity.Property(e => e.StatStr).HasColumnName("stat_str").HasDefaultValue(10);
            entity.Property(e => e.StatInt).HasColumnName("stat_int").HasDefaultValue(10);
            entity.Property(e => e.StatWis).HasColumnName("stat_wis").HasDefaultValue(10);
            entity.Property(e => e.StatVit).HasColumnName("stat_vit").HasDefaultValue(10);
            entity.Property(e => e.DailyDeadline).HasColumnName("daily_deadline").HasColumnType("time");
            entity.Property(e => e.FailureCount).HasColumnName("failure_count").HasDefaultValue(0);
            entity.Property(e => e.DebtAmount).HasColumnName("debt_amount").HasColumnType("decimal(10,2)").HasDefaultValue(0m);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp");
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.ToTable("goals");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.AttributeType)
                .HasColumnName("attribute_type")
                .HasConversion<string>()
                .HasColumnType("enum('STR','INT','WIS','VIT')")
                .IsRequired();
            entity.Property(e => e.Category)
                .HasColumnName("category")
                .HasConversion<string>()
                .HasColumnType("enum('RUNNING','STUDY','READING','GENERIC')");
            entity.Property(e => e.TargetMetric).HasColumnName("target_metric").HasMaxLength(255);
            entity.Property(e => e.ContentBacklog).HasColumnName("content_backlog").HasColumnType("text");
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasColumnType("enum('ACTIVE','COMPLETED','PAUSED')")
                .HasDefaultValue(GoalStatus.ACTIVE);
            entity.Property(e => e.ProgressNotes).HasColumnName("progress_notes").HasColumnType("text");
            entity.Property(e => e.StartDate).HasColumnName("start_date").HasColumnType("date");
            entity.Property(e => e.TargetDate).HasColumnName("target_date").HasColumnType("date");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Punishment>(entity =>
        {
            entity.ToTable("punishments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
            entity.Property(e => e.Category)
                .HasColumnName("category")
                .HasConversion<string>()
                .HasColumnType("enum('FISICO','ROTINA','FINANCEIRO')")
                .IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text").IsRequired();
            entity.Property(e => e.ProofType)
                .HasColumnName("proof_type")
                .HasConversion<string>()
                .HasColumnType("enum('STRAVA','SUMMARY','MINDMAP','PHOTO','PAYMENT_RECEIPT','GENERIC')");
            entity.Property(e => e.EstimatedMinutes).HasColumnName("estimated_minutes");
            entity.Property(e => e.XpReward).HasColumnName("xp_reward");
            entity.Property(e => e.AttributeType)
                .HasColumnName("attribute_type")
                .HasConversion<string>()
                .HasColumnType("enum('STR','INT','WIS','VIT')");
            entity.Property(e => e.Weight).HasColumnName("weight");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp");
        });

        modelBuilder.Entity<Quest>(entity =>
        {
            entity.ToTable("quests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.GoalId).HasColumnName("goal_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
            entity.Property(e => e.MetricTarget).HasColumnName("metric_target").HasMaxLength(255);
            entity.Property(e => e.ProofType)
                .HasColumnName("proof_type")
                .HasConversion<string>()
                .HasColumnType("enum('STRAVA','SUMMARY','MINDMAP','PHOTO','PAYMENT_RECEIPT','GENERIC')");
            entity.Property(e => e.EstimatedMinutes).HasColumnName("estimated_minutes");
            entity.Property(e => e.AttributeType)
                .HasColumnName("attribute_type")
                .HasConversion<string>()
                .HasColumnType("enum('STR','INT','WIS','VIT')")
                .IsRequired();
            entity.Property(e => e.XpReward).HasColumnName("xp_reward").HasDefaultValue(10);
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasColumnType("enum('PENDING','COMPLETED','FAILED')")
                .HasDefaultValue(QuestStatus.PENDING);
            entity.Property(e => e.IsPenalty).HasColumnName("is_penalty").HasDefaultValue(false);
            entity.Property(e => e.Kind)
                .HasColumnName("quest_kind")
                .HasConversion<string>()
                .HasColumnType("enum('NORMAL','PENALTY','BONUS')")
                .HasDefaultValue(QuestKind.NORMAL);
            entity.Property(e => e.TargetDate).HasColumnName("target_date").HasColumnType("date");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp");

            entity.HasOne(e => e.User)
                .WithMany(u => u.Quests)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Goal)
                .WithMany(g => g.Quests)
                .HasForeignKey(e => e.GoalId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Proof>(entity =>
        {
            entity.ToTable("proofs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.QuestId).HasColumnName("quest_id");
            entity.Property(e => e.FilePath).HasColumnName("file_path").HasMaxLength(255).IsRequired();
            entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at").HasColumnType("timestamp");
            entity.Property(e => e.AiVerified).HasColumnName("ai_verified").HasDefaultValue(false);

            entity.HasOne(e => e.Quest)
                .WithMany(q => q.Proofs)
                .HasForeignKey(e => e.QuestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestMetric>(entity =>
        {
            entity.ToTable("quest_metrics");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.QuestId).HasColumnName("quest_id");
            entity.Property(e => e.GoalId).HasColumnName("goal_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.MetricName).HasColumnName("metric_name").HasMaxLength(50).IsRequired();
            entity.Property(e => e.MetricValue).HasColumnName("metric_value").HasColumnType("decimal(10,2)");
            entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(20);
            entity.Property(e => e.RecordedAt).HasColumnName("recorded_at").HasColumnType("date");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp");

            entity.HasOne(e => e.Quest)
                .WithMany()
                .HasForeignKey(e => e.QuestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Goal)
                .WithMany()
                .HasForeignKey(e => e.GoalId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalityTrait>(entity =>
        {
            entity.ToTable("personality_traits");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TraitCode)
                .HasColumnName("trait_code")
                .HasConversion<string>()
                .HasColumnType("enum('OPENNESS','CONSCIENTIOUSNESS','EXTRAVERSION','AGREEABLENESS','NEUROTICISM')")
                .IsRequired();
            entity.Property(e => e.Value).HasColumnName("value").HasColumnType("decimal(5,2)").HasDefaultValue(50m);
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp");

            entity.HasIndex(e => new { e.UserId, e.TraitCode }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalityTraitHistory>(entity =>
        {
            entity.ToTable("personality_trait_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TraitCode)
                .HasColumnName("trait_code")
                .HasConversion<string>()
                .HasColumnType("enum('OPENNESS','CONSCIENTIOUSNESS','EXTRAVERSION','AGREEABLENESS','NEUROTICISM')")
                .IsRequired();
            entity.Property(e => e.Value).HasColumnName("value").HasColumnType("decimal(5,2)");
            entity.Property(e => e.Delta).HasColumnName("delta").HasColumnType("decimal(5,2)");
            entity.Property(e => e.RecordedAt).HasColumnName("recorded_at").HasColumnType("date");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp");

            entity.HasIndex(e => new { e.UserId, e.TraitCode, e.RecordedAt }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyReflection>(entity =>
        {
            entity.ToTable("daily_reflections");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ReflectionDate).HasColumnName("reflection_date").HasColumnType("date");
            entity.Property(e => e.AutopilotText).HasColumnName("autopilot_text").HasColumnType("text").IsRequired();
            entity.Property(e => e.IdealText).HasColumnName("ideal_text").HasColumnType("text");
            entity.Property(e => e.MentorFeedback).HasColumnName("mentor_feedback").HasColumnType("text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp");

            entity.HasIndex(e => new { e.UserId, e.ReflectionDate }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DailyBriefing>(entity =>
        {
            entity.ToTable("daily_briefings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BriefingDate).HasColumnName("briefing_date").HasColumnType("date");
            entity.Property(e => e.SummaryMd).HasColumnName("summary_md").HasColumnType("text").IsRequired();
            entity.Property(e => e.ItemsJson).HasColumnName("items_json").HasColumnType("text");
            entity.Property(e => e.SourceCount).HasColumnName("source_count");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp");

            entity.HasIndex(e => e.BriefingDate).IsUnique();
        });
    }
}
