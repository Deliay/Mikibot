using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Autofac.Extensions.DependencyInjection;

namespace Mikibot.Mirai.Util
{

    public class ContainerInitializer : ILoggingBuilder
    {
        public IServiceCollection Services { get; }
        public ContainerInitializer(IServiceCollection services)
        {
            Services = services;
        }

        public static ContainerBuilder Create()
        {
            var initializer = new ContainerInitializer(new ServiceCollection());
            initializer.Services.AddLogging();
            initializer.AddConsole();

            var autofac = new ContainerBuilder();
            autofac.Populate(initializer.Services);

            return autofac;
        }
    }
}
