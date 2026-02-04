using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public class TicketProcessingAgentRunner
{
    private readonly ITicketQueueService _ticketQueueService;
    private readonly IClassificationService _classificationService;
    private readonly IRoutingService _routingService;
    private readonly ISettingsProvider _settingsProvider;
    private readonly ILearningService _learningService;

    public TicketProcessingAgentRunner(
        ITicketQueueService ticketQueueService,
        IClassificationService classificationService,
        IRoutingService routingService,
        ISettingsProvider settingsProvider,
        ILearningService learningService)
    {
        _ticketQueueService = ticketQueueService ?? throw new ArgumentNullException(nameof(ticketQueueService));
        _classificationService = classificationService ?? throw new ArgumentNullException(nameof(classificationService));
        _routingService = routingService ?? throw new ArgumentNullException(nameof(routingService));
        _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        _learningService = learningService ?? throw new ArgumentNullException(nameof(learningService));
    }

    public async Task<TicketProcessingTickResult?> StepAsync(CancellationToken ct)
    {
        // 1) SENSE
        var ticket = await _ticketQueueService.DequeueNextQueuedAsync(ct);
        if (ticket == null)
        {
            return null; // NoWork
        }

        // 2) THINK
        var settings = await _settingsProvider.GetSettingsAsync(ct);
        var classification = _classificationService.Classify(ticket, settings);

        // Get category-specific threshold from learning service (uses feedback history)
        var effectiveThreshold = await _learningService.GetEffectiveThresholdAsync(classification.Category, ct);

        AgentDecision decision;
        SupportTeam? team = null;


        if (classification.MissingFields.Length > 0)
        {
            decision = settings.EnableAutoAskClarifyingQuestions
                ? AgentDecision.AskedForInfo
                : AgentDecision.SentToReview;
        }
        else if (classification.Confidence < effectiveThreshold)
        {
            // Use learned threshold per category instead of global threshold
            decision = AgentDecision.SentToReview;
        }
        else if (settings.EnableAutoAssign)
        {
            decision = AgentDecision.AutoAssigned;
            team = _routingService.Route(classification.Category);
        }
        else
        {
            // Auto-assign disabled -> send to review
            decision = AgentDecision.SentToReview;
        }

        // 3) ACT
        TicketStatus newStatus;
        string? questionText = null;
        string? reasonText = null;

        switch (decision)
        {
            case AgentDecision.AskedForInfo:
            {
                questionText = $"Please provide additional information: {string.Join(", ", classification.MissingFields)}";
                await _ticketQueueService.MarkWaitingForUserAsync(
                    ticket.Id,
                    questionText,
                    classification.MissingFields,
                    classification.Explanation,
                    ct);

                newStatus = TicketStatus.WaitingForUser;
                break;
            }

            case AgentDecision.AutoAssigned:
            {
                // team should always be set for AutoAssigned, but keep this safe.
                if (team == null)
                {
                    reasonText = "Routing failed (team was null). Sent to review.";
                    await _ticketQueueService.MarkNeedsReviewAsync(ticket.Id, reasonText, classification.Explanation, ct);
                    newStatus = TicketStatus.NeedsReview;
                    decision = AgentDecision.SentToReview;
                    break;
                }

                await _ticketQueueService.MarkAssignedAsync(
                    ticket.Id,
                    team.Value,
                    classification.Category,
                    classification.Priority,
                    classification.Confidence,
                    classification.Explanation,
                    ct);

                newStatus = TicketStatus.Assigned;
                break;
            }

            case AgentDecision.SentToReview:
            {
                if (classification.MissingFields.Length > 0 && !settings.EnableAutoAskClarifyingQuestions)
                {
                    reasonText =
                        $"Missing required fields ({string.Join(", ", classification.MissingFields)}) and auto-ask is disabled.";
                }
                else if (classification.Confidence < effectiveThreshold)
                {
                    reasonText = $"Low confidence ({classification.Confidence:F2} < {effectiveThreshold:F2} for {classification.Category}).";
                }
                else if (!settings.EnableAutoAssign)
                {
                    reasonText = "Auto-assign is disabled.";
                }
                else
                {
                    reasonText = "Sent to review by policy.";
                }

                await _ticketQueueService.MarkNeedsReviewAsync(ticket.Id, reasonText, classification.Explanation, ct);
                newStatus = TicketStatus.NeedsReview;
                break;
            }

            default:
            {
                // Do NOT throw inside an agent tick: fail safely and keep the worker alive.
                reasonText = $"Unexpected decision '{decision}'. Sent to review.";
                await _ticketQueueService.MarkNeedsReviewAsync(ticket.Id, reasonText, classification.Explanation, ct);
                newStatus = TicketStatus.NeedsReview;
                decision = AgentDecision.SentToReview;
                break;
            }
        }

        // 4) LEARN (light)
        // If you have a TicketEvent service later, this is the right place to log the decision as experience.
        // Example (future): await _eventService.RecordDecisionAsync(ticket.Id, decision, classification, ct);

        return new TicketProcessingTickResult
        {
            TicketId = ticket.Id,
            Decision = decision,
            NewStatus = newStatus,
            Category = classification.Category,
            Priority = classification.Priority,
            Team = team,
            Confidence = classification.Confidence,
            Explanation = classification.Explanation,
            MissingFields = classification.MissingFields.Length > 0 ? classification.MissingFields : null
        };
    }
}
