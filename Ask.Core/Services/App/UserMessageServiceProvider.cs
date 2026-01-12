using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Services.App
{
  /// <summary>
  /// Провайдер сервиса отображения сообщений пользователю.
  /// </summary>
  public static class UserMessageServiceProvider
  {
    private static IMessageOutputService? _instance;

    /// <summary>
    /// Событие, вызываемое при смене активного экземпляра IMessageService.
    /// </summary>
    public static event Action<IMessageOutputService?>? InstanceChanged;

    /// <summary>
    /// Текущая реализация интерфейса отображения сообщений.
    /// </summary>
    public static IMessageOutputService? Instance
    {
      get => _instance;
      set
      {
        if (!ReferenceEquals(_instance, value))
        {
          _instance = value;
          InstanceChanged?.Invoke(_instance);
        }
      }
    }

    /// <summary>
    /// Асинхронный вызов отображения сообщения.
    /// </summary>
    public static Task ShowMessageAsync(ShowMessageModel model, bool IsBlockStart = false, bool SkipStepModeCheck = false)
    {
      return Instance?.ShowMessageAsync(model, IsBlockStart, SkipStepModeCheck) ?? Task.FromResult(false);
    }
  }
}
