using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public class TicketProcessingTickResult
{
    public Guid TicketId { get; set; }
    public AgentDecision Decision { get; set; }
    public TicketStatus NewStatus { get; set; }
    public TicketCategory? Category { get; set; }
    public TicketPriority? Priority { get; set; }
    public SupportTeam? Team { get; set; }
    public double? Confidence { get; set; }
    public string? Explanation { get; set; }
    public string[]? MissingFields { get; set; }
}

