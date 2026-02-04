using Microsoft.EntityFrameworkCore;
using AiAgents.HelpdeskAgent.Domain;
using AiAgents.HelpdeskAgent.Infrastructure;

namespace AiAgents.HelpdeskAgent.Application;

/// <summary>
/// Implements adaptive learning based on human feedback.
/// Adjusts per-category confidence thresholds to improve decision accuracy over time.
/// </summary>
public class LearningService : ILearningService
{
    private readonly HelpdeskDbContext _context;
    private readonly ISettingsProvider _settingsProvider;

    // Learning rate parameters
    private const double IncreaseOnWrong = 0.05;  // Increase threshold when wrong (be more cautious)
    private const double DecreaseOnCorrect = 0.02; // Decrease threshold when correct (be more confident)
    private const double MinThreshold = 0.3;       // Never go below this (always maintain some caution)
    private const double MaxThreshold = 0.95;      // Never go above this (always allow some auto-assign)

    public LearningService(HelpdeskDbContext context, ISettingsProvider settingsProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
    }

    public async Task LearnFromFeedbackAsync(FeedbackEntry feedback, CancellationToken ct = default)
    {
        // Get or create policy parameter for the original category (the one we classified as)
        var policyParam = await _context.CategoryPolicyParameters
            .FirstOrDefaultAsync(p => p.Category == feedback.OriginalCategory, ct);

        if (policyParam == null)
        {
            // Initialize with global threshold
            var globalSettings = await _settingsProvider.GetSettingsAsync(ct);
            policyParam = new CategoryPolicyParameter
            {
                Category = feedback.OriginalCategory,
                ConfidenceThreshold = globalSettings.ConfidenceThreshold,
                TotalFeedbackCount = 0,
                CorrectCount = 0,
                IncorrectCount = 0,
                LastUpdated = DateTime.UtcNow
            };
            _context.CategoryPolicyParameters.Add(policyParam);
        }

        // Update statistics
        policyParam.TotalFeedbackCount++;
        policyParam.LastUpdated = DateTime.UtcNow;

        if (feedback.WasCategoryCorrect)
        {
            // Correct classification → decrease threshold (be more confident)
            policyParam.CorrectCount++;
            policyParam.ConfidenceThreshold = Math.Max(
                MinThreshold,
                policyParam.ConfidenceThreshold - DecreaseOnCorrect
            );
        }
        else
        {
            // Wrong classification → increase threshold (be more cautious, send more to review)
            policyParam.IncorrectCount++;
            policyParam.ConfidenceThreshold = Math.Min(
                MaxThreshold,
                policyParam.ConfidenceThreshold + IncreaseOnWrong
            );
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<double> GetEffectiveThresholdAsync(TicketCategory category, CancellationToken ct = default)
    {
        // Try to get category-specific threshold
        var policyParam = await _context.CategoryPolicyParameters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Category == category, ct);

        if (policyParam != null)
        {
            return policyParam.ConfidenceThreshold;
        }

        // Fall back to global threshold
        var globalSettings = await _settingsProvider.GetSettingsAsync(ct);
        return globalSettings.ConfidenceThreshold;
    }

    public async Task<IReadOnlyList<CategoryPolicyParameter>> GetAllPolicyParametersAsync(CancellationToken ct = default)
    {
        return await _context.CategoryPolicyParameters
            .AsNoTracking()
            .OrderBy(p => p.Category)
            .ToListAsync(ct);
    }
}
