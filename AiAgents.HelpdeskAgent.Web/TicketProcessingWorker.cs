using Microsoft.Extensions.Hosting;
using AiAgents.HelpdeskAgent.Application;

namespace AiAgents.HelpdeskAgent.Web;

public class TicketProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TicketProcessingWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<TicketProcessingAgentRunner>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<TicketProcessingWorker>>();

            logger.LogInformation("Background worker tick executing at {Time}", DateTime.UtcNow);

            try
            {
                var result = await runner.StepAsync(stoppingToken);
                if (result != null)
                {
                    logger.LogInformation(
                        "Processed Ticket {TicketId} Decision={Decision} NewStatus={NewStatus} Category={Category} Priority={Priority} Team={Team} Confidence={Confidence}",
                        result.TicketId, result.Decision, result.NewStatus, result.Category, result.Priority, result.Team, result.Confidence);
                }
                else
                {
                    logger.LogDebug("No tickets in queue, waiting for next tick");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing ticket");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}
