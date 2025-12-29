using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public class SettingsProvider : ISettingsProvider
{
    public Task<AgentSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new AgentSettings
        {
            Id = 0,
            ConfidenceThreshold = 0.7,
            EnableAutoAssign = true,
            EnableAutoAskClarifyingQuestions = true
        });
    }
}

