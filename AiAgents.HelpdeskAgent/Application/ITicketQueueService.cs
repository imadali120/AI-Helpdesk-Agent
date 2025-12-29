using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public interface ITicketQueueService
{
    Task EnqueueAsync(Ticket ticket, CancellationToken ct);
    Task<Ticket?> DequeueNextQueuedAsync(CancellationToken ct);
    Task MarkWaitingForUserAsync(Guid ticketId, string question, string[] missingFields, string? explanation, CancellationToken ct);
    Task MarkNeedsReviewAsync(Guid ticketId, string reason, string? explanation, CancellationToken ct);
    Task MarkAssignedAsync(Guid ticketId, SupportTeam team, TicketCategory category, TicketPriority priority, double confidence, string? explanation, CancellationToken ct);
}

