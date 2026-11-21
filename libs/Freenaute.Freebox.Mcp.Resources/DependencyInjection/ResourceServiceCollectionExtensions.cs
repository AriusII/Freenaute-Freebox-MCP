namespace Freenaute.Freebox.Mcp.Resources.DependencyInjection;

public static class ResourceServiceCollectionExtensions
{
    public static IMcpServerBuilder AddFreeboxResources(this IMcpServerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .WithResources<DocumentationResources>()
            .WithResources<LiveResources>();
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddProductionFreeboxLiveResourceSource()
        {
            ArgumentNullException.ThrowIfNull(services);
            return services.AddSingleton<IFreeboxLiveResourceSource, OrchestratorFreeboxLiveResourceSource>();
        }

        public IServiceCollection AddFreeboxLiveResourceSource(IFreeboxLiveResourceSource source)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(source);

            return services.AddSingleton(source);
        }

        public IServiceCollection AddFreeboxLiveResourceSource(
            Func<IServiceProvider, IFreeboxLiveResourceSource> factory)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(factory);

            return services.AddSingleton(factory);
        }
    }
}
