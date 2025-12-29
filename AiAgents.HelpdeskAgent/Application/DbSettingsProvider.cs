using Microsoft.EntityFrameworkCore;
using AiAgents.HelpdeskAgent.Domain;
using AiAgents.HelpdeskAgent.Infrastructure;

namespace AiAgents.HelpdeskAgent.Application;

public class DbSettingsProvider : ISettingsProvider
{
    private readonly HelpdeskDbContext _context;

    public DbSettingsProvider(HelpdeskDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<AgentSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var settings = await _context.AgentSettings.FirstOrDefaultAsync(ct);

        if (settings == null)
        {
            // Return sensible defaults if no row exists
            return new AgentSettings
            {
                Id = 0,
                ConfidenceThreshold = 0.7,
                EnableAutoAssign = true,
                EnableAutoAskClarifyingQuestions = true
            };
        }

        return settings;
    }
}

