namespace AiAgents.HelpdeskAgent.Domain;

/// <summary>
/// Per-category policy parameters that are adjusted through learning.
/// Each category can have its own confidence threshold based on feedback history.
/// </summary>
public class CategoryPolicyParameter
{
    public int Id { get; set; }

    /// <summary>
    /// The ticket category this parameter applies to.
    /// </summary>
    public TicketCategory Category { get; set; }

    /// <summary>
    /// The confidence threshold for this category.
    /// If classification confidence is below this, ticket goes to review.
    /// Adjusted by learning: wrong → increase, correct → decrease.
    /// </summary>
    public double ConfidenceThreshold { get; set; }

    /// <summary>
    /// Total number of feedback entries processed for this category.
    /// </summary>
    public int TotalFeedbackCount { get; set; }

    /// <summary>
    /// Number of correct classifications for this category.
    /// </summary>
    public int CorrectCount { get; set; }

    /// <summary>
    /// Number of incorrect classifications for this category.
    /// </summary>
    public int IncorrectCount { get; set; }

    /// <summary>
    /// Last time this parameter was updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
