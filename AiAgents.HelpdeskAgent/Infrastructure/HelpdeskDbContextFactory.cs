using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiAgents.HelpdeskAgent.Infrastructure;

public class HelpdeskDbContextFactory : IDesignTimeDbContextFactory<HelpdeskDbContext>
{
    public HelpdeskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HelpdeskDbContext>();
        optionsBuilder.UseSqlite("Data Source=helpdesk.db");

        return new HelpdeskDbContext(optionsBuilder.Options);
    }
}

