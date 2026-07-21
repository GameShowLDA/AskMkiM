using Ask.Core.Shared.DTO.Executor;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.ProtocolNew.Execution
{
  /// <summary>
  /// Хранит настройки текущего режима выполнения и выполняет их первоначальную нормализацию.
  /// Передаёт исполнителю исходный экземпляр <see cref="ActionSettings"/> без копирования.
  /// </summary>
  internal sealed class ExecutionModeSettings
  {
    /// <summary>Настройки текущего режима выполнения.</summary>
    private ActionSettings? _current;

    /// <summary>
    /// Возвращает настроенный экземпляр текущего режима.
    /// До вызова <see cref="Configure"/> значение не определено.
    /// </summary>
    public ActionSettings Current => _current!;

    /// <summary>
    /// Возвращает признак автоматического накопления ошибочных сообщений в итоговом заключении.
    /// </summary>
    public bool AccumulateErrorMessages => _current?.AccumulateErrorMessages == true;

    /// <summary>
    /// Сохраняет и нормализует настройки выбранного режима выполнения.
    /// </summary>
    /// <param name="settings">Исходные настройки режима.</param>
    /// <param name="modeName">Отображаемое имя режима.</param>
    public void Configure(ActionSettings settings, string modeName)
    {
      try
      {
        _current = settings;
        _current.Name = modeName;
      }
      catch (Exception exception)
      {
        LogException("Ошибка загрузки элемента", exception);
        throw;
      }
    }

    /// <summary>Очищает ошибки, накопленные настройками текущего режима.</summary>
    public void ClearExecutionErrors()
    {
      _current?.ExecutionErrors.Clear();
    }
  }
}
