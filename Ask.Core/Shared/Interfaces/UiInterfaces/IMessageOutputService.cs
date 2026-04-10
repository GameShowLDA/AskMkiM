using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using System.Runtime.CompilerServices;

namespace Ask.Core.Shared.Interfaces.UiInterfaces
{
  public interface IMessageOutputService : ITextAdapter
  {
    /// <summary>
    /// Асинхронно отображает сообщение пользователю с учётом режима выполнения,
    /// форматирования блоков и возможностей пошагового режима.
    /// </summary>
    /// <param name="model">Модель содержимого отображаемого сообщения.</param>
    /// <param name="IsBlockStart">Признак начала логического блока для форматирования.</param>
    /// <param name="SkipStepModeCheck">
    /// Указывает, следует ли игнорировать ожидание пользовательского действия в
    /// пошаговом режиме.
    /// </param>
    /// <param name="skipPause">Пропустить ли автоматическую паузу перед отображением.</param>
    /// <param name="callerName">Имя вызывающего метода (устанавливается автоматически).</param>
    /// <param name="callerFile">Файл вызова метода (автоматически).</param>
    /// <param name="callerLine">Строка вызова метода (автоматически).</param>
    /// <returns>Задача, представляющая операцию отображения сообщения.</returns>
    Task ShowMessageAsync(
      ShowMessageModel model,
      bool IsBlockStart = false,
      bool SkipStepModeCheck = false,
      bool skipPause = false,
      [CallerMemberName] string callerName = "",
      [CallerFilePath] string callerFile = "",
      [CallerLineNumber] int callerLine = 0);

    /// <summary>
    /// Добавляет пустую строку.
    /// </summary>
    Task AppendEmptyLineAsync(int indentLevel = 0);

    /// <summary>
    /// Завершает текущую команду протокола и передаёт её итоговый результат.
    /// Используется для подсветки заголовка команды независимо от видимости внутренних строк.
    /// </summary>
    /// <param name="hasErrors">
    /// <see langword="true"/>, если команда завершилась с ошибками; иначе <see langword="false"/>.
    /// </param>
    Task CompleteCommandAsync(bool hasErrors);

    /// <summary>
    /// Заголовок текущего сообщения.
    /// </summary>
    string Header { get; set; }

    /// <summary>
    /// Получает номер последней строки вывода.
    /// </summary>
    int GetLastLineNumber();

    /// <summary>
    /// Переходит к указанной строке.
    /// </summary>
    Task MoveToLineAsync(int lineNumber);
  }
}
