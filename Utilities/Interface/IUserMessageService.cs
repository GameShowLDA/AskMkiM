using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Models;

namespace Utilities.Interface
{
  /// <summary>
  /// Интерфейс для отображения сообщений пользователю и управления действиями, доступными при ошибках.
  /// </summary>
  public interface IUserMessageService
  {
    /// <summary>
    /// Перечисление возможных действий пользователя при ошибке.
    /// </summary>
    public enum UserAction
    {
      Retry,
      Continue,
      Abort,
      None,
    }
    public IButtonService ButtonService { get; set; }

    /// <summary>
    /// Асинхронно отображает сообщение пользователю.
    /// </summary>
    /// <param name="model">Модель отображаемого сообщения.</param>
    /// <param name="IsBlockStart">Указывает, считать ли это сообщение началом логического блока (для форматирования).</param>
    /// <param name="SkipStepModeCheck">Указывает, следует ли пропускать ожидание пользовательского действия в пошаговом режиме.</param>
    /// <returns>Задача, представляющая асинхронную операцию отображения.</returns>
    Task ShowMessageAsync(ShowMessageModel model, bool IsBlockStart = false, bool SkipStepModeCheck = false, bool skipPause = false);

    /// <summary>
    /// Асинхронно добавляет пустую строку в вывод сообщений.
    /// </summary>
    /// <param name="indentLevel">Уровень отступа строки (для визуального выравнивания).</param>
    Task AppendEmptyLineAsync(int indentLevel = 0);

    /// <summary>
    /// Возвращает или задаёт текущий заголовок сообщения, отображаемого пользователю.
    /// </summary>
    string Header { get; set; }

    /// <summary>
    /// Ожидает подтверждение действия пользователем (например, нажатием кнопки администратора).
    /// </summary>
    /// <returns>True, если пользователь подтвердил действие; иначе — false.</returns>
    Task<bool> WaitAdminButtonAsync();

    /// <summary>
    /// Асинхронно ожидает выбора пользователя (повторить, продолжить, завершить) после сообщения.
    /// </summary>
    /// <param name="canRetry">Показывать кнопку "Повторить".</param>
    /// <param name="canContinue">Показывать кнопку "Продолжить".</param>
    /// <param name="canAbort">Показывать кнопку "Завершить".</param>
    /// <returns>Выбранное пользователем действие.</returns>
    Task<UserAction> WaitUserActionAsync();
  }
}
