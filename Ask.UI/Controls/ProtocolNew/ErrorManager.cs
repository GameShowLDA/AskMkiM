using Ask.Core.Services.Errors.Models;
using Ask.UI.Controls.ErrorList;
using Ask.UI.Features.ProtocolNew.Errors;

namespace Ask.UI.Controls.ProtocolNew
{
  /// <summary>
  /// Сохраняет прежний публичный контракт управления списком ошибок
  /// и делегирует работу менеджеру подсистемы <c>Features.ProtocolNew</c>.
  /// </summary>
  public class ErrorManager
  {
    /// <summary>Менеджер состояния и представления списка ошибок.</summary>
    private readonly ProtocolErrorManager _manager;

    /// <summary>
    /// Добавляет ошибку в список протокола.
    /// </summary>
    /// <param name="errorItem">Добавляемая ошибка.</param>
    public void AddError(ErrorItem errorItem)
    {
      _manager.AddError(errorItem);
    }

    /// <summary>Очищает список и сбрасывает количество ошибок.</summary>
    internal void ErrorClear()
    {
      _manager.Clear();
    }

    /// <summary>
    /// Создаёт совместимый менеджер для существующего элемента списка ошибок.
    /// </summary>
    /// <param name="errorListBoxVertical">Элемент отображения ошибок.</param>
    public ErrorManager(ErrorListControl errorListBoxVertical)
    {
      _manager = new ProtocolErrorManager(new ErrorListControlAdapter(errorListBoxVertical));
    }

    /// <summary>
    /// Создаёт менеджер для представления, предоставленного <see cref="ProtocolUI"/>.
    /// </summary>
    /// <param name="view">Представление списка ошибок.</param>
    internal ErrorManager(IProtocolErrorListView view)
    {
      _manager = new ProtocolErrorManager(view);
    }

    /// <summary>
    /// Адаптирует существующий <see cref="ErrorListControl"/> к контракту подсистемы ошибок.
    /// </summary>
    private sealed class ErrorListControlAdapter : IProtocolErrorListView
    {
      /// <summary>Адаптируемый элемент управления.</summary>
      private readonly ErrorListControl _control;

      /// <summary>
      /// Создаёт адаптер визуального списка.
      /// </summary>
      /// <param name="control">Существующий элемент списка ошибок.</param>
      public ErrorListControlAdapter(ErrorListControl control)
      {
        _control = control;
      }

      /// <inheritdoc />
      public void AddError(ErrorItem errorItem) => _control.AddError(errorItem);

      /// <inheritdoc />
      public void ClearErrors() => _control.ClearAll();
    }
  }
}
