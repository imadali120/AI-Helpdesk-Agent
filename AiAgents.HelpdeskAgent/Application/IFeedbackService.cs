using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public interface IFeedbackService
{
    Task SubmitFeedbackAsync(Guid ticketId, TicketCategory correctCategory, TicketPriority correctPriority, string? note, CancellationToken ct);
}

