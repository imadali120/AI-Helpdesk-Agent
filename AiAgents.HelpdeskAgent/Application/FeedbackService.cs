using Microsoft.EntityFrameworkCore;
using AiAgents.HelpdeskAgent.Domain;
using AiAgents.HelpdeskAgent.Infrastructure;

namespace AiAgents.HelpdeskAgent.Application;

public class FeedbackService : IFeedbackService
{
    private readonly HelpdeskDbContext _context;
    private readonly ILearningService _learningService;

    public FeedbackService(HelpdeskDbContext context, ILearningService learningService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _learningService = learningService ?? throw new ArgumentNullException(nameof(learningService));
    }

    public async Task SubmitFeedbackAsync(
        Guid ticketId,
        TicketCategory correctCategory,
        TicketPriority correctPriority,
        string? note,
        CancellationToken ct)
    {
        // 1) Get the ticket to retrieve original classification
        var ticket = await _context.Tickets.FindAsync(new object[] { ticketId }, ct);
        if (ticket == null)
        {
            throw new InvalidOperationException($"Ticket {ticketId} not found.");
        }

        // Use the ticket's current category/priority as "original" (what the agent assigned)
        // If ticket hasn't been classified yet, use a default
        var originalCategory = ticket.Category ?? TicketCategory.Other;
        var originalPriority = ticket.Priority ?? TicketPriority.Medium;

        // Determine if classifications were correct
        bool wasCategoryCorrect = originalCategory == correctCategory;
        bool wasPriorityCorrect = originalPriority == correctPriority;

        // 2) Create and persist the feedback entry
        var feedbackEntry = new FeedbackEntry
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Timestamp = DateTime.UtcNow,
            OriginalCategory = originalCategory,
            OriginalPriority = originalPriority,
            CorrectCategory = correctCategory,
            CorrectPriority = correctPriority,
            WasCategoryCorrect = wasCategoryCorrect,
            WasPriorityCorrect = wasPriorityCorrect,
            Note = note
        };

        _context.FeedbackEntries.Add(feedbackEntry);
        await _context.SaveChangesAsync(ct);

        // 3) Trigger learning - adjust policy parameters based on feedback
        await _learningService.LearnFromFeedbackAsync(feedbackEntry, ct);

        // 4) Optionally update the ticket with corrected values
        if (!wasCategoryCorrect || !wasPriorityCorrect)
        {
            ticket.Category = correctCategory;
            ticket.Priority = correctPriority;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }
}
