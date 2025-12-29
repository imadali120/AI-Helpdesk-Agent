using AiAgents.HelpdeskAgent.Domain;

namespace AiAgents.HelpdeskAgent.Application;

public class RoutingService : IRoutingService
{
    public SupportTeam Route(TicketCategory category)
    {
        return category switch
        {
            TicketCategory.Account => SupportTeam.AccountsTeam,
            TicketCategory.Billing => SupportTeam.BillingTeam,
            TicketCategory.Technical => SupportTeam.TechTeam,
            TicketCategory.Other => SupportTeam.GeneralSupport,
            _ => SupportTeam.GeneralSupport
        };
    }
}

