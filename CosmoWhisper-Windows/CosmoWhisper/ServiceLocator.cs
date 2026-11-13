using Microsoft.Extensions.DependencyInjection;

namespace CosmoWhisper
{
    public static class ServiceLocator
    {
        public static IServiceProvider? Provider { get; set; }

        public static T GetService<T>() where T : notnull =>
            (Provider ?? throw new InvalidOperationException("ServiceLocator.Provider has not been initialized."))
            .GetRequiredService<T>();
    }
}