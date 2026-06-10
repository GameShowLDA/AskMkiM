using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Collectors;
using Ask.Diagnostics.Configuration;
using Ask.Diagnostics.Models;
using Ask.Diagnostics.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ask.Diagnostics.Extensions
{
  public static class ServiceCollectionExtensions
  {
    public static IServiceCollection AddCrashDiagnostics(
      this IServiceCollection services,
      Action<CrashPackageOptions>? configure = null,
      Action<string>? logInformation = null,
      Action<Exception, string>? logError = null,
      Action<string>? crashPackageCreated = null)
    {
      ArgumentNullException.ThrowIfNull(services);

      if (configure != null)
      {
        services.Configure(configure);
      }
      else
      {
        services.Configure<CrashPackageOptions>(_ => { });
      }

      services.AddSingleton<ICrashPackageLogSink>(_ => new DelegateCrashPackageLogSink(logInformation, logError, crashPackageCreated));
      services.AddSingleton<ICommandHistoryService, CommandHistoryService>();
      services.AddHostedService<CommandHistoryBridgeHostedService>();
      services.AddSingleton<ICrashPackageService, CrashPackageService>();
      services.AddSingleton<IExceptionDiagnosticReporter, ExceptionDiagnosticReporter>();

      services.AddSingleton<ICrashDataCollector, ExceptionCollector>();
      services.AddSingleton<ICrashDataCollector, ScreenshotCollector>();
      services.AddSingleton<ICrashDataCollector, CommandHistoryCollector>();
      services.AddSingleton<ICrashDataCollector, DeviceStateCollector>();
      services.AddSingleton<ICrashDataCollector, ConfigCollector>();
      services.AddSingleton<ICrashDataCollector, LogCollector>();
      services.AddSingleton<ICrashDataCollector, SystemInfoCollector>();
      services.AddSingleton<ICrashDataCollector, MetadataCollector>();

      return services;
    }

    public static IServiceCollection AddDiagnosticStateProvider(
      this IServiceCollection services,
      string name,
      Func<CrashContext, CancellationToken, Task<object?>> capture)
    {
      ArgumentNullException.ThrowIfNull(services);
      ArgumentNullException.ThrowIfNull(capture);

      services.AddSingleton<IDiagnosticStateProvider>(_ => new DelegateDiagnosticStateProvider(name, capture));
      return services;
    }

    public static IServiceCollection AddDiagnosticStateProvider(
      this IServiceCollection services,
      string name,
      Func<object?> capture)
    {
      ArgumentNullException.ThrowIfNull(capture);
      return services.AddDiagnosticStateProvider(name, (_, _) => Task.FromResult(capture()));
    }

    public static IServiceCollection AddDiagnosticConfigProvider(
      this IServiceCollection services,
      string name,
      Func<CrashContext, CancellationToken, Task<object?>> capture)
    {
      ArgumentNullException.ThrowIfNull(services);
      ArgumentNullException.ThrowIfNull(capture);

      services.AddSingleton<IDiagnosticConfigProvider>(_ => new DelegateDiagnosticConfigProvider(name, capture));
      return services;
    }

    public static IServiceCollection AddDiagnosticConfigProvider(
      this IServiceCollection services,
      string name,
      Func<object?> capture)
    {
      ArgumentNullException.ThrowIfNull(capture);
      return services.AddDiagnosticConfigProvider(name, (_, _) => Task.FromResult(capture()));
    }
  }
}
