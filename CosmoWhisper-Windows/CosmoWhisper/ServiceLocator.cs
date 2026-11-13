using Microsoft.Extensions.DependencyInjection;

namespace CosmoWhisper
{
    public static class ServiceLocator
    {
        public static IServiceProvider Provider { get; set; }

        public static T GetService<T>() where T : notnull => Provider.GetRequiredService<T>();
    }
}