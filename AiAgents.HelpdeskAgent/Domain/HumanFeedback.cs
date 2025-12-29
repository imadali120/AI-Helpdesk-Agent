namespace AiAgents.HelpdeskAgent.Domain;

public class HumanFeedback
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Comment { get; set; }
    public bool? IsApproved { get; set; }
}

