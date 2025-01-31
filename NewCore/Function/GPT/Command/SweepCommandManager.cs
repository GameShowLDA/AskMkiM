namespace NewCore.Function.GPT.Command
{
  /// <summary>
  /// Класс для управления командами развертки.
  /// </summary>
  internal class SweepCommandManager
  {
    /// <summary>
    /// Перечисление команд развертки устройства.
    /// </summary>
    public enum SweepCommand
    {
      /// <summary>
      /// Возврат данных теста.
      /// </summary>
      SWEEP_DATA_STATUS,

      /// <summary>
      /// Возврат объединенных данных (от 1 до 190 (single data point))
      /// </summary>
      SWEEP_DATA_SHOW,

      /// <summary>
      /// Показать графы в дисплее (ON или OFF)
      /// </summary>
      SWEEP_GRAPH_SHOW,

      /// <summary>
      /// Установка/возврат линий, которые отображаются в графах.
      /// 0 Выключите все линии/все линии выключены.
      /// 1 Отображает линию графика для основного тестового элемента. Подробности см. на стр. 77. Например: V для тестов ACW, DCW и IR, I для тестов GB.
      /// 2 Отображает линию графика для дополнительных тестовых заданий. Например: I для тестов ACW и DCW, R для тестов IR и GB.
      /// 3 Включите все линии/все линии включены.
      /// </summary>
      SWEEP_GRAPH_LINE,

      /// <summary>
      /// Установка/возврат времени старта для графов в секундах (от 0.1 до 1999.8 seconds)
      /// </summary>
      SWEEP_START_TIME,
    }

    /// <summary>
    /// Словарь для получения синтаксиса команды по ее типу.
    /// </summary>
    public static readonly Dictionary<SweepCommand, string> CommandSyntax = new Dictionary<SweepCommand, string>
    {
        { SweepCommand.SWEEP_DATA_STATUS, "SWEEP:DATA:STAT" },
        { SweepCommand.SWEEP_DATA_SHOW, "SWEEP:DATA:SHOW" }, // <X> может быть параметром, который вы добавите при использовании
        { SweepCommand.SWEEP_GRAPH_SHOW, "SWEEP:GRAP:SHOW" },
        { SweepCommand.SWEEP_GRAPH_LINE, "SWEEP:GRAP:LINE" },
        { SweepCommand.SWEEP_START_TIME, "SWEEP:STAR:TIME" },
    };

    /// <summary>
    /// Получает строку команды по ключу из словаря.
    /// </summary>
    /// <param name="command">Ключ команды из перечисления SweepCommand.</param>
    /// <returns>Строка команды, соответствующая ключу.</returns>
    public static string GetCommandSyntax(SweepCommand command)
    {
      if (CommandSyntax.TryGetValue(command, out var syntax))
      {
        return syntax;
      }
      else
      {
        throw new ArgumentException("Команда не найдена в словаре.", nameof(command));
      }
    }
  }
}
