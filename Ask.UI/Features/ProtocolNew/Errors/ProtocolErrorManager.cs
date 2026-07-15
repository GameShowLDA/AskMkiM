using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Adapters;
using Ask.UI.Shared.Formatting;
using System.Windows;

namespace Ask.UI.Features.ProtocolNew.Errors
{
  /// <summary>
  /// Управляет счётчиком и визуальным списком ошибок протокола выполнения.
  /// </summary>
  internal sealed class ProtocolErrorManager
  {
    /// <summary>Представление списка ошибок.</summary>
    private readonly IProtocolErrorListView _view;

    /// <summary>Текущее количество зарегистрированных ошибок.</summary>
    private int _errorCount;

    /// <summary>
    /// Создаёт менеджер визуального списка ошибок.
    /// </summary>
    /// <param name="view">Представление списка ошибок.</param>
    public ProtocolErrorManager(IProtocolErrorListView view)
    {
      _view = view;
    }

    /// <summary>
    /// Добавляет ошибку в UI-потоке и публикует обновлённое общее количество.
    /// </summary>
    /// <param name="errorItem">Добавляемая ошибка.</param>
    public void AddError(ErrorItem errorItem)
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        _view.AddError(errorItem);
        _errorCount++;
        MessageEventAdapter.RaiseInfoMessage(
          $"Общее кол-во ошибок: {CountDisplayFormatter.Format(_errorCount)}");
      });
    }

    /// <summary>Очищает список и сбрасывает счётчик ошибок в UI-потоке.</summary>
    public void Clear()
    {
      Application.Current.Dispatcher?.Invoke(() =>
      {
        _view.ClearErrors();
        _errorCount = 0;
      });
    }
  }
}
