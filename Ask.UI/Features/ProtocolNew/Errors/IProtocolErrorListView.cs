using Ask.Core.Services.Errors.Models;

namespace Ask.UI.Features.ProtocolNew.Errors
{
  /// <summary>
  /// Предоставляет менеджеру ошибок минимальные операции визуального списка.
  /// </summary>
  internal interface IProtocolErrorListView
  {
    /// <summary>
    /// Добавляет ошибку в визуальный список.
    /// </summary>
    /// <param name="errorItem">Отображаемая ошибка.</param>
    void AddError(ErrorItem errorItem);

    /// <summary>Удаляет все ошибки из визуального списка.</summary>
    void ClearErrors();
  }
}
