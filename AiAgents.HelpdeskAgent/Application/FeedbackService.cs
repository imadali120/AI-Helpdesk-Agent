using System.Collections.Concurrent;
using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public class FeedbackService : IFeedbackService
{
    private readonly ConcurrentBag<FeedbackEntry> _feedbackEntries = new();

    public Task SubmitFeedbackAsync(Guid ticketId, TicketCategory correctCategory, TicketPriority correctPriority, string? note, CancellationToken ct)
    {
        var feedback = new FeedbackEntry
        {
            TicketId = ticketId,
            CorrectCategory = correctCategory,
            CorrectPriority = correctPriority,
            Note = note,
            Timestamp = DateTime.UtcNow
        };

        _feedbackEntries.Add(feedback);

        return Task.CompletedTask;
    }

    // Internal class to store feedback in memory
    private class FeedbackEntry
    {
        public Guid TicketId { get; set; }
        public TicketCategory CorrectCategory { get; set; }
        public TicketPriority CorrectPriority { get; set; }
        public string? Note { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

