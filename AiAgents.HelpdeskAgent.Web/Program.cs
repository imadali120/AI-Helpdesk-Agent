using Microsoft.EntityFrameworkCore;
using AiAgents.HelpdeskAgent.Application;
using AiAgents.HelpdeskAgent.Infrastructure;
using AiAgents.HelpdeskAgent.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext
builder.Services.AddDbContext<HelpdeskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("HelpdeskDb")));

// Register Infrastructure services
builder.Services.AddScoped<DatabaseSeeder>();

// Register Application services
builder.Services.AddScoped<ITicketQueueService, TicketQueueService>();
builder.Services.AddScoped<IClassificationService, ClassificationService>();
builder.Services.AddScoped<IRoutingService, RoutingService>();
builder.Services.AddScoped<ISettingsProvider, DbSettingsProvider>();
builder.Services.AddScoped<ILearningService, LearningService>();  // Must be before FeedbackService
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<TicketProcessingAgentRunner>();

// Register Background Worker
builder.Services.AddHostedService<TicketProcessingWorker>();

var app = builder.Build();

// Apply migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();
    await db.Database.MigrateAsync();
    
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
