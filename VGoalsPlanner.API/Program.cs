using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VGoalsPlanner.API.Startup;
using VGoalsPlanner.Application;
using VGoalsPlanner.Application.Abstractions.Data;
using VGoalsPlanner.Infrastructure.Persistence.Database;



var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.IncludeFields = true;
    });

builder.Services.AddDbContextFactory<VGoalsPlannerDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("GoalPlannerDbConnection"), npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    });
});

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<VGoalsPlannerDbContext>());
builder.Services.AddApplication();
builder.Services.AddDependencies();
builder.Services.AddVStorage(builder.Configuration);
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAuthorization(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<VGoalsPlannerDbContext>();

            var migrator = db.Database.GetService<IMigrator>();
            migrator.Migrate();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
