using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public interface IRoutingService
{
    SupportTeam Route(TicketCategory category);
}

