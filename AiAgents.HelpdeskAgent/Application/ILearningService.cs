using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

/// <summary>
/// Service responsible for learning from human feedback and adjusting policy parameters.
/// </summary>
public interface ILearningService
{
    /// <summary>
    /// Processes feedback and adjusts the confidence threshold for the relevant category.
    /// Wrong classification → increase threshold (be more cautious).
    /// Correct classification → decrease threshold (be more confident).
    /// </summary>
    Task LearnFromFeedbackAsync(FeedbackEntry feedback, CancellationToken ct = default);

    /// <summary>
    /// Gets the effective confidence threshold for a specific category.
    /// Returns category-specific threshold if available, otherwise falls back to global threshold.
    /// </summary>
    Task<double> GetEffectiveThresholdAsync(TicketCategory category, CancellationToken ct = default);

    /// <summary>
    /// Gets all category policy parameters for reporting/monitoring.
    /// </summary>
    Task<IReadOnlyList<CategoryPolicyParameter>> GetAllPolicyParametersAsync(CancellationToken ct = default);
}
