using System;
using System.Threading.Tasks;
using Utilities.Models;

namespace AppConfiguration.Interface
{
  /// <summary>
  /// Интерфейс для отображения сообщений пользователю и управления действиями, доступными при ошибках.
  /// </summary>
  public interface IUserMessageService
  {
    /// <summary>
    /// Асинхронно отображает сообщение пользователю.
    /// </summary>
    /// <param name="model">Модель отображаемого сообщения.</param>
    /// <param name="IsBlockStart">Указывает, считать ли это сообщение началом логического блока (для форматирования).</param>
    /// <param name="SkipStepModeCheck">Указывает, следует ли пропускать ожидание пользовательского действия в пошаговом режиме.</param>
    /// <returns>Задача, представляющая асинхронную операцию отображения.</returns>
    Task ShowMessageAsync(ShowMessageModel model, bool IsBlockStart = false, bool SkipStepModeCheck = false);

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
    /// Регистрирует асинхронное действие, которое может быть повторено пользователем в случае ошибки.
    /// </summary>
    /// <param name="retryAction">Делегат действия, которое можно повторить (например, шаг замыкания цепи).</param>
    void RegisterRetryAction(Func<Task> retryAction);

    /// <summary>
    /// Выполняет зарегистрированное действие повтора, если оно задано.
    /// </summary>
    /// <returns>Задача, представляющая выполнение повтора. Если повтора нет — ничего не происходит.</returns>
    Task TryInvokeRetryAsync();

    /// <summary>
    /// Очищает сохранённое действие повтора.
    /// </summary>
    void ClearRetryAction();

    /// <summary>
    /// Возвращает признак того, что зарегистрировано действие, доступное для повтора.
    /// </summary>
    bool HasRetryAction { get; }
  }
}
