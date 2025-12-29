using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AiAgents.HelpdeskAgent.Application;
using AiAgents.HelpdeskAgent.Domain;
using AiAgents.HelpdeskAgent.Infrastructure;

namespace AiAgents.HelpdeskAgent.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketQueueService _ticketQueueService;
    private readonly IFeedbackService _feedbackService;
    private readonly HelpdeskDbContext _context;

    public TicketsController(
        ITicketQueueService ticketQueueService,
        IFeedbackService feedbackService,
        HelpdeskDbContext context)
    {
        _ticketQueueService = ticketQueueService ?? throw new ArgumentNullException(nameof(ticketQueueService));
        _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Subject = request.Subject,
            Body = request.Body,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = TicketStatus.Queued
        };

        await _ticketQueueService.EnqueueAsync(ticket, ct);

        return Accepted(new CreateTicketResponse
        {
            TicketId = ticket.Id,
            Status = "Queued"
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicket(Guid id, CancellationToken ct)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { id }, ct);
        
        if (ticket == null)
        {
            return NotFound();
        }

        // Load related TicketEvents to get the latest note
        var latestEvent = await _context.TicketEvents
            .Where(e => e.TicketId == id)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync(ct);

        // Parse missing fields from RequiredFieldsMissing (comma-separated string)
        string[]? missingFields = null;
        if (!string.IsNullOrWhiteSpace(ticket.RequiredFieldsMissing))
        {
            missingFields = ticket.RequiredFieldsMissing
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();
        }

        return Ok(new TicketResponse
        {
            Id = ticket.Id,
            Status = ticket.Status,
            Category = ticket.Category,
            Priority = ticket.Priority,
            AssignedTeam = ticket.AssignedTeam,
            Confidence = ticket.Confidence,
            RequiredFieldsMissing = ticket.RequiredFieldsMissing,
            UpdatedAt = ticket.UpdatedAt,
            Explanation = latestEvent?.Explanation,
            MissingFields = missingFields,
            LatestNote = latestEvent?.Description
        });
    }

    [HttpPost("{id}/feedback")]
    public async Task<IActionResult> SubmitFeedback(Guid id, [FromBody] SubmitFeedbackRequest request, CancellationToken ct)
    {
        await _feedbackService.SubmitFeedbackAsync(id, request.CorrectCategory, request.CorrectPriority, request.Note, ct);
        return Ok();
    }
}

public class CreateTicketRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class CreateTicketResponse
{
    public Guid TicketId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TicketResponse
{
    public Guid Id { get; set; }
    public TicketStatus Status { get; set; }
    public TicketCategory? Category { get; set; }
    public TicketPriority? Priority { get; set; }
    public SupportTeam? AssignedTeam { get; set; }
    public double? Confidence { get; set; }
    public string? RequiredFieldsMissing { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Explanation { get; set; }
    public string[]? MissingFields { get; set; }
    public string? LatestNote { get; set; }
}

public class SubmitFeedbackRequest
{
    public TicketCategory CorrectCategory { get; set; }
    public TicketPriority CorrectPriority { get; set; }
    public string? Note { get; set; }
}

