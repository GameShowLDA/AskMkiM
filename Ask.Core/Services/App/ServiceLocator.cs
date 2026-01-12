using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ask.Core.Services.App
{
  public static class ServiceLocator
  {
    public static IHost Host { get; private set; }
    public static IServiceProvider Services => Host?.Services
        ?? throw new InvalidOperationException("ServiceLocator.Host не инициализирован.");

    public static void Initialize(IHost host)
    {
      Host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public static T GetRequired<T>() where T : notnull =>
      Services.GetRequiredService<T>();

    public static T? Get<T>() =>
      Services.GetService<T>();

    /// <summary>
    /// Пытается получить сервис. Если сервис не зарегистрирован — возвращает null,
    /// без выброса исключений.
    /// </summary>
    public static T? TryGet<T>() =>
        Services.GetService<T>();

    public static void Reset() => Host = null;
  }
}
