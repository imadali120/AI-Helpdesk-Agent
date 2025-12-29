using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public interface IClassificationService
{
    ClassificationResult Classify(Ticket ticket, AgentSettings settings);
}

