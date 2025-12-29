namespace AiAgents.HelpdeskAgent.Domain;

public class TicketEvent
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Explanation { get; set; }
}

