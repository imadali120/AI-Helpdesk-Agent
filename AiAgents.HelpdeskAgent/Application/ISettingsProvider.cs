using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public interface ISettingsProvider
{
    Task<AgentSettings> GetSettingsAsync(CancellationToken ct = default);
}

