using Microsoft.Extensions.DependencyInjection;
using XLabApp.Services.Interfaces;

namespace XLabApp.Services
{
    internal static class ServiceRegistrator
    {
        public static IServiceCollection AddServices(this IServiceCollection services) => services
           .AddTransient<IDataService, DataService>();
    }
}
