using Momus.Config;

namespace Momus.Cli.Services;

public interface IRouteConfigService
{
    Task<YarpConfig> LoadConfigAsync(CancellationToken cancellationToken = default);
    Task SaveConfigAsync(YarpConfig config, CancellationToken cancellationToken = default);
    bool ValidateConfig(YarpConfig config);
    
    // Cached config for in-memory operations
    YarpConfig? GetCachedConfig();
    void SetCachedConfig(YarpConfig config);
}
