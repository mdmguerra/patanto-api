using PatentsViewAPI.Configuration;
using PatentsViewAPI.Services;
using PatentsViewAPI.Services.Interfaces;

namespace PatentsViewAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPatentsViewServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure options
            services.Configure<PatentsViewOptions>(
                configuration.GetSection(PatentsViewOptions.SectionName));

            // Register HttpClient for PatentsViewService
            services.AddHttpClient<IPatentsViewService, PatentsViewService>();

            return services;
        }
    }
}
