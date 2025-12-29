using Microsoft.EntityFrameworkCore;
using AiAgents.HelpdeskAgent.Domain;
using AiAgents.HelpdeskAgent.Infrastructure;

namespace AiAgents.HelpdeskAgent.Application;

public class TicketQueueService : ITicketQueueService
{
    private readonly HelpdeskDbContext _context;

    public TicketQueueService(HelpdeskDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task EnqueueAsync(Ticket ticket, CancellationToken ct)
    {
        if (ticket == null)
            throw new ArgumentNullException(nameof(ticket));

        var now = DateTime.UtcNow;

        if (ticket.CreatedAt == default)
            ticket.CreatedAt = now;

        ticket.UpdatedAt = now;
        ticket.Status = TicketStatus.Queued;

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<Ticket?> DequeueNextQueuedAsync(CancellationToken ct)
    {
        // Use a transaction to ensure atomicity and prevent double-processing
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            // Small bounded retry to avoid "false NoWork" if another worker grabs the same oldest item
            for (var attempt = 0; attempt < 3; attempt++)
            {
                // Find the ID of the oldest ticket with Status == Queued
                var ticketId = await _context.Tickets
                    .Where(t => t.Status == TicketStatus.Queued)
                    .OrderBy(t => t.CreatedAt)
                    .Select(t => t.Id)
                    .FirstOrDefaultAsync(ct);

                if (ticketId == Guid.Empty)
                {
                    await transaction.CommitAsync(ct);
                    return null;
                }

                var now = DateTime.UtcNow;

                // Atomically update status to Processing only if it's still Queued
                var updatedCount = await _context.Tickets
                    .Where(t => t.Id == ticketId && t.Status == TicketStatus.Queued)
                    .ExecuteUpdateAsync(
                        setter => setter
                            .SetProperty(t => t.Status, TicketStatus.Processing)
                            .SetProperty(t => t.UpdatedAt, now),
                        ct);

                if (updatedCount == 0)
                {
                    // Another worker likely took it; try again (bounded)
                    continue;
                }

                // Reload the updated ticket
                var ticket = await _context.Tickets.FindAsync(new object[] { ticketId }, ct);
                await transaction.CommitAsync(ct);

                return ticket;
            }

            // Could not claim a ticket after retries
            await transaction.CommitAsync(ct);
            return null;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task MarkWaitingForUserAsync(Guid ticketId, string question, string[] missingFields, string? explanation, CancellationToken ct)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { ticketId }, ct);
        if (ticket == null)
            throw new InvalidOperationException($"Ticket {ticketId} not found");

        // Idempotency-friendly: if already in this state with same missing fields, do nothing.
        var missingJoined = string.Join(", ", missingFields ?? Array.Empty<string>());
        if (ticket.Status == TicketStatus.WaitingForUser &&
            string.Equals(ticket.RequiredFieldsMissing ?? string.Empty, missingJoined, StringComparison.Ordinal))
        {
            return;
        }

        ticket.Status = TicketStatus.WaitingForUser;
        ticket.RequiredFieldsMissing = missingJoined;
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.TicketEvents.Add(new TicketEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Timestamp = DateTime.UtcNow,
            EventType = "WaitingForUser",
            Description = question,
            Explanation = explanation
        });

        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkNeedsReviewAsync(Guid ticketId, string reason, string? explanation, CancellationToken ct)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { ticketId }, ct);
        if (ticket == null)
            throw new InvalidOperationException($"Ticket {ticketId} not found");

        // Idempotency-friendly: if already NeedsReview with same reason, do nothing.
        if (ticket.Status == TicketStatus.NeedsReview)
        {
            // (Optional) You could also store last reason somewhere; for now, avoid duplicate events on repeated ticks.
            return;
        }

        ticket.Status = TicketStatus.NeedsReview;
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.TicketEvents.Add(new TicketEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Timestamp = DateTime.UtcNow,
            EventType = "NeedsReview",
            Description = reason,
            Explanation = explanation
        });

        await _context.SaveChangesAsync(ct);
    }

    public async Task MarkAssignedAsync(
        Guid ticketId,
        SupportTeam team,
        TicketCategory category,
        TicketPriority priority,
        double confidence,
        string? explanation,
        CancellationToken ct)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { ticketId }, ct);
        if (ticket == null)
            throw new InvalidOperationException($"Ticket {ticketId} not found");

        // Idempotency-friendly: if already assigned with same core fields, do nothing.
        if (ticket.Status == TicketStatus.Assigned &&
            ticket.AssignedTeam == team &&
            ticket.Category == category &&
            ticket.Priority == priority &&
            Math.Abs((ticket.Confidence ?? 0d) - confidence) < 0.0001d)
        {
            return;
        }

        ticket.Status = TicketStatus.Assigned;
        ticket.AssignedTeam = team;
        ticket.Category = category;
        ticket.Priority = priority;
        ticket.Confidence = confidence;
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.TicketEvents.Add(new TicketEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Timestamp = DateTime.UtcNow,
            EventType = "Assigned",
            Description = $"Assigned to {team}, Category: {category}, Priority: {priority}, Confidence: {confidence:F2}",
            Explanation = explanation
        });

        await _context.SaveChangesAsync(ct);
    }
}
