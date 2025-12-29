using Microsoft.EntityFrameworkCore;
using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Infrastructure;

public class DatabaseSeeder
{
    private readonly HelpdeskDbContext _context;

    public DatabaseSeeder(HelpdeskDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Seed AgentSettings if not exists
        if (!await _context.AgentSettings.AnyAsync(ct))
        {
            var defaultSettings = new AgentSettings
            {
                Id = 1,
                ConfidenceThreshold = 0.7,
                EnableAutoAssign = true,
                EnableAutoAskClarifyingQuestions = true
            };

            _context.AgentSettings.Add(defaultSettings);
        }

        // Seed sample tickets if not exists
        var existingTicketCount = await _context.Tickets.CountAsync(ct);
        if (existingTicketCount == 0)
        {
            var sampleTickets = new[]
            {
                new Ticket
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CustomerId = "CUST-001",
                    Subject = "Cannot login to my account",
                    Body = "I forgot my password and cannot sign in. Please help me reset it.",
                    Status = TicketStatus.Queued
                },
                new Ticket
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CustomerId = "CUST-002",
                    Subject = "Billing question",
                    Body = "I was charged twice this month. Can you please refund the duplicate payment?",
                    Status = TicketStatus.Queued
                },
                new Ticket
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CustomerId = "CUST-003",
                    Subject = "Application error",
                    Body = "The app crashes when I try to open the settings page. Error message says 'NullReferenceException'.",
                    Status = TicketStatus.Queued
                },
                new Ticket
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CustomerId = "CUST-004",
                    Subject = "Feature request",
                    Body = "How can I export my data? I don't see this option anywhere.",
                    Status = TicketStatus.Queued
                },
                new Ticket
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CustomerId = "CUST-005",
                    Subject = "URGENT: Security issue",
                    Body = "I noticed unauthorized access to my account. This is urgent!",
                    Status = TicketStatus.Queued
                }
            };

            _context.Tickets.AddRange(sampleTickets);
        }

        await _context.SaveChangesAsync(ct);
    }
}

