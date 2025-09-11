// AppConfiguration/ServiceLocator.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppConfiguration;

public static class ServiceLocator
{
  // Храним Host (удобно для StopAsync/Dispose)
  public static IHost Host { get; private set; }
  public static IServiceProvider Services => Host?.Services
      ?? throw new InvalidOperationException("ServiceLocator.Host не инициализирован.");

  // Инициализация из App.OnStartup
  public static void Initialize(IHost host)
  {
    Host = host ?? throw new ArgumentNullException(nameof(host));
  }

  // Удобные хелперы
  public static T GetRequired<T>() where T : notnull =>
    Services.GetRequiredService<T>();

  public static T? Get<T>() =>
    Services.GetService<T>();

  // По желанию — сброс (для тестов)
  public static void Reset() => Host = null;
}
