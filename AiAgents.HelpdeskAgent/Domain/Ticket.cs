namespace AiAgents.HelpdeskAgent.Domain;

public class Ticket
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketCategory? Category { get; set; }
    public TicketPriority? Priority { get; set; }
    public SupportTeam? AssignedTeam { get; set; }
    public double? Confidence { get; set; }
    public AgentDecision? LastAgentDecision { get; set; }
    public string? RequiredFieldsMissing { get; set; }
}

