using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiAgents.HelpdeskAgent.Application;
using AiAgents.HelpdeskAgent.Domain;
using AiAgents.HelpdeskAgent.Infrastructure;

namespace AiAgents.HelpdeskAgent.Web.Controllers;

/// <summary>
/// Demo endpoint to prove the learning mechanism works.
/// Shows before/after feedback threshold and decision changes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LearningDemoController : ControllerBase
{
    private readonly HelpdeskDbContext _context;
    private readonly ITicketQueueService _ticketQueueService;
    private readonly IClassificationService _classificationService;
    private readonly IFeedbackService _feedbackService;
    private readonly ILearningService _learningService;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IRoutingService _routingService;

    public LearningDemoController(
        HelpdeskDbContext context,
        ITicketQueueService ticketQueueService,
        IClassificationService classificationService,
        IFeedbackService feedbackService,
        ILearningService learningService,
        ISettingsProvider settingsProvider,
        IRoutingService routingService)
    {
        _context = context;
        _ticketQueueService = ticketQueueService;
        _classificationService = classificationService;
        _feedbackService = feedbackService;
        _learningService = learningService;
        _settingsProvider = settingsProvider;
        _routingService = routingService;
    }

    /// <summary>
    /// Demonstrates that the same ticket gets different decisions after feedback.
    ///
    /// Scenario:
    /// 1. Process ticket → classified as Technical with confidence ~0.75 → AutoAssigned
    /// 2. Submit feedback: "This was actually Billing" (wrong classification)
    /// 3. Technical threshold increases from 0.70 → 0.75
    /// 4. Process identical ticket → same confidence but now below threshold → SentToReview
    /// </summary>
    [HttpPost("run-demo")]
    public async Task<IActionResult> RunDemo(CancellationToken ct)
    {
        var log = new List<string>();

        log.Add("=== LEARNING MECHANISM DEMO ===");
        log.Add("");

        // Get initial settings
        var settings = await _settingsProvider.GetSettingsAsync(ct);
        log.Add($"[INIT] Global ConfidenceThreshold: {settings.ConfidenceThreshold:F2}");

        // Check initial threshold for Technical category
        var initialThreshold = await _learningService.GetEffectiveThresholdAsync(TicketCategory.Technical, ct);
        log.Add($"[INIT] Technical category threshold: {initialThreshold:F2}");
        log.Add("");

        // ========== STEP 1: Create and process first ticket ==========
        log.Add("--- STEP 1: Process ticket BEFORE feedback ---");

        var ticket1 = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = "demo-customer",
            Subject = "Application error on startup",
            Body = "I get an error message when the application starts. It says something crashed.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = TicketStatus.Queued
        };

        // Classify the ticket
        var classification1 = _classificationService.Classify(ticket1, settings);
        var threshold1 = await _learningService.GetEffectiveThresholdAsync(classification1.Category, ct);

        log.Add($"  Ticket Subject: \"{ticket1.Subject}\"");
        log.Add($"  Classified as: {classification1.Category}");
        log.Add($"  Confidence: {classification1.Confidence:F2}");
        log.Add($"  Effective Threshold: {threshold1:F2}");

        // Determine decision
        AgentDecision decision1;
        if (classification1.Confidence >= threshold1 && settings.EnableAutoAssign)
        {
            decision1 = AgentDecision.AutoAssigned;
        }
        else
        {
            decision1 = AgentDecision.SentToReview;
        }

        log.Add($"  DECISION: {decision1}");
        log.Add($"  Reason: confidence {classification1.Confidence:F2} {'≥' } threshold {threshold1:F2} → {(classification1.Confidence >= threshold1 ? "above" : "below")} threshold");

        // Save ticket with classification
        ticket1.Category = classification1.Category;
        ticket1.Priority = classification1.Priority;
        ticket1.Confidence = classification1.Confidence;
        ticket1.Status = decision1 == AgentDecision.AutoAssigned ? TicketStatus.Assigned : TicketStatus.NeedsReview;
        ticket1.AssignedTeam = decision1 == AgentDecision.AutoAssigned ? _routingService.Route(classification1.Category) : null;

        _context.Tickets.Add(ticket1);
        await _context.SaveChangesAsync(ct);

        log.Add("");

        // ========== STEP 2: Submit WRONG feedback ==========
        log.Add("--- STEP 2: Submit feedback (WRONG classification) ---");
        log.Add($"  Human says: 'This is actually Billing, not Technical'");
        log.Add($"  Original: {classification1.Category}, Correct: Billing");

        // Submit feedback - say it was actually Billing (wrong classification for Technical)
        await _feedbackService.SubmitFeedbackAsync(
            ticket1.Id,
            TicketCategory.Billing,  // Correct category according to human
            classification1.Priority,
            "Demo: This was actually a billing issue, not technical.",
            ct);

        // Check new threshold
        var newThreshold = await _learningService.GetEffectiveThresholdAsync(TicketCategory.Technical, ct);
        log.Add($"  Technical threshold BEFORE feedback: {threshold1:F2}");
        log.Add($"  Technical threshold AFTER feedback:  {newThreshold:F2}");
        log.Add($"  Change: +{(newThreshold - threshold1):F2} (increased because wrong → be more cautious)");
        log.Add("");

        // ========== STEP 3: Process IDENTICAL ticket ==========
        log.Add("--- STEP 3: Process IDENTICAL ticket AFTER feedback ---");

        var ticket2 = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = "demo-customer-2",
            Subject = "Application error on startup",  // Same subject
            Body = "I get an error message when the application starts. It says something crashed.",  // Same body
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = TicketStatus.Queued
        };

        // Classify the second ticket (will get same classification)
        var classification2 = _classificationService.Classify(ticket2, settings);
        var threshold2 = await _learningService.GetEffectiveThresholdAsync(classification2.Category, ct);

        log.Add($"  Ticket Subject: \"{ticket2.Subject}\"");
        log.Add($"  Classified as: {classification2.Category}");
        log.Add($"  Confidence: {classification2.Confidence:F2}");
        log.Add($"  Effective Threshold: {threshold2:F2} (was {threshold1:F2})");

        // Determine decision with NEW threshold
        AgentDecision decision2;
        if (classification2.Confidence >= threshold2 && settings.EnableAutoAssign)
        {
            decision2 = AgentDecision.AutoAssigned;
        }
        else
        {
            decision2 = AgentDecision.SentToReview;
        }

        log.Add($"  DECISION: {decision2}");
        log.Add($"  Reason: confidence {classification2.Confidence:F2} {(classification2.Confidence >= threshold2 ? "≥" : "<")} threshold {threshold2:F2}");
        log.Add("");

        // ========== SUMMARY ==========
        log.Add("=== SUMMARY ===");
        log.Add($"  Same ticket text, same classification confidence ({classification1.Confidence:F2})");
        log.Add($"  Threshold changed: {threshold1:F2} → {threshold2:F2}");
        log.Add($"  Decision changed: {decision1} → {decision2}");
        log.Add("");

        if (decision1 != decision2)
        {
            log.Add("✓ LEARNING WORKS! Same ticket got different decision after feedback.");
        }
        else
        {
            log.Add("Note: Decision didn't change (threshold change wasn't enough to flip decision).");
            log.Add("Run demo multiple times to accumulate threshold changes.");
        }

        // Get all policy parameters for display
        var allParams = await _learningService.GetAllPolicyParametersAsync(ct);
        if (allParams.Any())
        {
            log.Add("");
            log.Add("=== CURRENT POLICY PARAMETERS (per category) ===");
            foreach (var p in allParams)
            {
                log.Add($"  {p.Category}: threshold={p.ConfidenceThreshold:F2}, correct={p.CorrectCount}, incorrect={p.IncorrectCount}");
            }
        }

        return Ok(new { log = log, success = decision1 != decision2 });
    }

    /// <summary>
    /// Reset learning state for demo purposes.
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetLearning(CancellationToken ct)
    {
        // Delete all policy parameters
        var allParams = await _context.CategoryPolicyParameters.ToListAsync(ct);
        _context.CategoryPolicyParameters.RemoveRange(allParams);

        // Delete all feedback entries
        var allFeedback = await _context.FeedbackEntries.ToListAsync(ct);
        _context.FeedbackEntries.RemoveRange(allFeedback);

        // Delete demo tickets
        var demoTickets = await _context.Tickets
            .Where(t => t.CustomerId.StartsWith("demo-"))
            .ToListAsync(ct);
        _context.Tickets.RemoveRange(demoTickets);

        await _context.SaveChangesAsync(ct);

        return Ok(new { message = "Learning state reset. Policy parameters cleared." });
    }

    /// <summary>
    /// View current policy parameters.
    /// </summary>
    [HttpGet("policy-parameters")]
    public async Task<IActionResult> GetPolicyParameters(CancellationToken ct)
    {
        var settings = await _settingsProvider.GetSettingsAsync(ct);
        var allParams = await _learningService.GetAllPolicyParametersAsync(ct);

        var result = new
        {
            globalThreshold = settings.ConfidenceThreshold,
            categoryThresholds = allParams.Select(p => new
            {
                category = p.Category.ToString(),
                threshold = p.ConfidenceThreshold,
                totalFeedback = p.TotalFeedbackCount,
                correct = p.CorrectCount,
                incorrect = p.IncorrectCount,
                lastUpdated = p.LastUpdated
            })
        };

        return Ok(result);
    }
}
