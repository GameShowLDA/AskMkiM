using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using System.Windows.Controls;

namespace Ask.Core.Shared.Interfaces.UiInterfaces
{
  /// <summary>
  /// Интерфейс для отображения сообщений пользователю и управления действиями, доступными при ошибках.
  /// </summary>
  public interface IUserInteractionService : IMessageOutputService
  {
    IButtonService ButtonService { get; set; }

    /// <summary>
    /// Асинхронно ожидает выбора пользователя (повторить, продолжить, завершить) после сообщения.
    /// </summary>
    /// <returns>Выбранное пользователем действие.</returns>
    Task<UserAction> WaitUserActionAsync(bool loop = false, bool deviceTask = false);

    CancellationToken GetCancellationToken();

    void AddError(ErrorItem errorItem);

    UserControl GetControl();
  }
}
