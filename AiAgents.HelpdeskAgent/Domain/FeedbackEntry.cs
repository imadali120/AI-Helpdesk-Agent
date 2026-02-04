namespace AiAgents.HelpdeskAgent.Domain;

/// <summary>
/// Persistent record of human feedback on agent classification decisions.
/// Used by the learning mechanism to adjust policy parameters.
/// </summary>
public class FeedbackEntry
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The category the agent originally assigned.
    /// </summary>
    public TicketCategory OriginalCategory { get; set; }

    /// <summary>
    /// The priority the agent originally assigned.
    /// </summary>
    public TicketPriority OriginalPriority { get; set; }

    /// <summary>
    /// The correct category according to human reviewer.
    /// </summary>
    public TicketCategory CorrectCategory { get; set; }

    /// <summary>
    /// The correct priority according to human reviewer.
    /// </summary>
    public TicketPriority CorrectPriority { get; set; }

    /// <summary>
    /// True if the agent's category classification was correct.
    /// </summary>
    public bool WasCategoryCorrect { get; set; }

    /// <summary>
    /// True if the agent's priority classification was correct.
    /// </summary>
    public bool WasPriorityCorrect { get; set; }

    /// <summary>
    /// Optional note from the reviewer.
    /// </summary>
    public string? Note { get; set; }
}
