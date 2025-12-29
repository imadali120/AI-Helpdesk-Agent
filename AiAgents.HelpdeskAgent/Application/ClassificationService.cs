using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public class ClassificationService : IClassificationService
{
    public ClassificationResult Classify(Ticket ticket, AgentSettings settings)
    {
        if (ticket == null)
            throw new ArgumentNullException(nameof(ticket));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        var combinedText = $"{ticket.Subject} {ticket.Body}".ToLowerInvariant();
        var explanationParts = new List<string>();
        var missingFields = new List<string>();

        // Determine Category
        var category = DetermineCategory(combinedText, explanationParts);

        // Determine Priority
        var priority = DeterminePriority(combinedText, explanationParts);

        // Check for missing fields
        CheckMissingFields(ticket, category, combinedText, missingFields);

        // Calculate Confidence
        var confidence = CalculateConfidence(combinedText, category, priority);

        var explanation = explanationParts.Count > 0
            ? string.Join("; ", explanationParts)
            : "No strong keywords matched";

        return new ClassificationResult
        {
            Category = category,
            Priority = priority,
            Confidence = confidence,
            MissingFields = missingFields.ToArray(),
            Explanation = explanation
        };
    }

    private TicketCategory DetermineCategory(string text, List<string> explanationParts)
    {
        var accountKeywords = new[] { "login", "password", "account", "sign in" };
        var billingKeywords = new[] { "charged", "invoice", "payment", "refund" };
        var technicalKeywords = new[] { "error", "bug", "crash", "not working", "cannot" };

        var accountMatches = accountKeywords.Count(k => text.Contains(k));
        var billingMatches = billingKeywords.Count(k => text.Contains(k));
        var technicalMatches = technicalKeywords.Count(k => text.Contains(k));

        if (accountMatches > 0 && accountMatches >= billingMatches && accountMatches >= technicalMatches)
        {
            explanationParts.Add($"Account category ({accountMatches} keyword matches)");
            return TicketCategory.Account;
        }

        if (billingMatches > 0 && billingMatches >= technicalMatches)
        {
            explanationParts.Add($"Billing category ({billingMatches} keyword matches)");
            return TicketCategory.Billing;
        }

        if (technicalMatches > 0)
        {
            explanationParts.Add($"Technical category ({technicalMatches} keyword matches)");
            return TicketCategory.Technical;
        }

        return TicketCategory.Other;
    }

    private TicketPriority DeterminePriority(string text, List<string> explanationParts)
    {
        var urgentKeywords = new[] { "urgent", "asap", "down", "security" };
        var highKeywords = new[] { "cannot", "blocked", "failed" };

        if (urgentKeywords.Any(k => text.Contains(k)))
        {
            explanationParts.Add("Urgent priority (urgent keywords detected)");
            return TicketPriority.Urgent;
        }

        if (highKeywords.Any(k => text.Contains(k)))
        {
            explanationParts.Add("High priority (high-priority keywords detected)");
            return TicketPriority.High;
        }

        // Check if it's a generic question (low priority indicator)
        var questionIndicators = new[] { "how", "what", "where", "when", "why", "?" };
        if (questionIndicators.Any(k => text.Contains(k)) && !text.Contains("error") && !text.Contains("bug"))
        {
            explanationParts.Add("Low priority (generic question)");
            return TicketPriority.Low;
        }

        explanationParts.Add("Medium priority (default)");
        return TicketPriority.Medium;
    }

    private void CheckMissingFields(Ticket ticket, TicketCategory category, string text, List<string> missingFields)
    {
        // If Technical and message contains "error" but no obvious code/description length < 20
        if (category == TicketCategory.Technical && text.Contains("error"))
        {
            var bodyLength = ticket.Body?.Length ?? 0;
            var hasCode = text.Contains("exception") || text.Contains("stack") || text.Contains("trace") || 
                         text.Contains("line") || text.Contains("at ") || text.Contains("system.");
            
            if (bodyLength < 20 || !hasCode)
            {
                missingFields.Add("error_details");
            }
        }

        // If user says "doesn't work" with no context
        if ((text.Contains("doesn't work") || text.Contains("does not work") || text.Contains("not working")) &&
            !text.Contains("when") && !text.Contains("after") && !text.Contains("step"))
        {
            missingFields.Add("steps_to_reproduce");
        }
    }

    private double CalculateConfidence(string text, TicketCategory category, TicketPriority priority)
    {
        var confidence = 0.6; // Baseline

        // Add confidence for category keywords
        var categoryKeywords = category switch
        {
            TicketCategory.Account => new[] { "login", "password", "account", "sign in" },
            TicketCategory.Billing => new[] { "charged", "invoice", "payment", "refund" },
            TicketCategory.Technical => new[] { "error", "bug", "crash", "not working", "cannot" },
            _ => Array.Empty<string>()
        };

        var strongCategoryMatch = categoryKeywords.Any(k => text.Contains(k));
        if (strongCategoryMatch)
        {
            confidence += 0.15;
        }

        // Add confidence for priority keywords
        var priorityKeywords = priority switch
        {
            TicketPriority.Urgent => new[] { "urgent", "asap", "down", "security" },
            TicketPriority.High => new[] { "cannot", "blocked", "failed" },
            _ => Array.Empty<string>()
        };

        if (priorityKeywords.Any(k => text.Contains(k)))
        {
            confidence += 0.10;
        }

        return Math.Min(confidence, 0.95);
    }
}

