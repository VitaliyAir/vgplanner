using VGoalsPlanner.API.Auth;
using VGoalsPlanner.API.Services;
using VGoalsPlanner.Application.Abstractions.Services;

namespace VGoalsPlanner.API.Startup
{
    public static class DependencyRegistrator
    {
        public static void AddDependencies(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddTransient<ICurrentUserService, CurrentUserService>();
            services.AddSingleton<UserProvisioner>();
        }
    }
}
