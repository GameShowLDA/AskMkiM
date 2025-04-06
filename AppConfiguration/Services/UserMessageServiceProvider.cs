using System;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using Utilities.Models;

namespace AppConfiguration.Services
{
  /// <summary>
  /// Провайдер сервиса отображения сообщений пользователю.
  /// </summary>
  public static class UserMessageServiceProvider
  {
    private static IUserMessageService? _instance;

    /// <summary>
    /// Событие, вызываемое при смене активного экземпляра IMessageService.
    /// </summary>
    public static event Action<IUserMessageService?>? InstanceChanged;

    /// <summary>
    /// Текущая реализация интерфейса отображения сообщений.
    /// </summary>
    public static IUserMessageService? Instance
    {
      get => _instance;
      set
      {
        if (!ReferenceEquals(_instance, value))
        {
          Console.WriteLine($"Активный элемент сменен на \"{value.Header}\"");
          _instance = value;
          InstanceChanged?.Invoke(_instance);
        }
      }
    }

    /// <summary>
    /// Асинхронный вызов отображения сообщения.
    /// </summary>
    public static Task<bool> ShowMessageAsync(ShowMessageModel model)
    {
      return Instance?.ShowMessageAsync(model) ?? Task.FromResult(false);
    }
  }
}
