using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public class ClassificationResult
{
    public TicketCategory Category { get; set; }
    public TicketPriority Priority { get; set; }
    public double Confidence { get; set; }
    public string[] MissingFields { get; set; } = Array.Empty<string>();
    public string? Explanation { get; set; }
}

