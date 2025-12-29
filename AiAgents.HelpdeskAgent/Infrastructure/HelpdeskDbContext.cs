using Microsoft.EntityFrameworkCore;
using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Infrastructure;

public class HelpdeskDbContext : DbContext
{
    public HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<TicketEvent> TicketEvents { get; set; } = null!;
    public DbSet<AgentSettings> AgentSettings { get; set; } = null!;
    public DbSet<HumanFeedback> HumanFeedback { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ticket configuration
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.Subject).IsRequired();
            entity.Property(e => e.Body).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasConversion<int>();
            entity.Property(e => e.Category).HasConversion<int>();
            entity.Property(e => e.Priority).HasConversion<int>();
            entity.Property(e => e.AssignedTeam).HasConversion<int>();
            entity.Property(e => e.LastAgentDecision).HasConversion<int>();
        });

        // TicketEvent configuration
        modelBuilder.Entity<TicketEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TicketId).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.Explanation);
        });

        // AgentSettings configuration
        modelBuilder.Entity<AgentSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ConfidenceThreshold).IsRequired();
            entity.Property(e => e.EnableAutoAssign).IsRequired();
            entity.Property(e => e.EnableAutoAskClarifyingQuestions).IsRequired();
        });

        // HumanFeedback configuration
        modelBuilder.Entity<HumanFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.TicketId).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
        });
    }
}

