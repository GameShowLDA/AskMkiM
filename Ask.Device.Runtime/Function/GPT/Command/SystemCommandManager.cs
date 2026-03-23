namespace NewCore.Function.GPT.Command
{
  /// <summary>
  /// Класс для управления системными командами.
  /// </summary>
  static internal class SystemCommandManager
  {
    /// <summary>
    /// Системны команды ППУ.
    /// </summary>
    internal enum SystemCommand
    {
      /// <summary>
      /// Установка контраста дисплея (от 1 до 8).
      /// </summary>
      LCD_CONTRAST,

      /// <summary>
      /// Установка яркости дисплея (1- dark, 2 - bright).
      /// </summary>
      LCD_BRIGHTNESS,

      /// <summary>
      /// Включение/выключение звука успешного теста (ON , OFF).
      /// </summary>
      BUZZER_PSOUND,

      /// <summary>
      /// Включение/выключение звука ошибочного теста (ON , OFF).
      /// </summary>
      BUZZER_FSOUND,

      /// <summary>
      /// Установка продолжительности звука успешного теста (0.2 - 999.9).
      /// </summary>
      BUZZER_PTIME,

      /// <summary>
      /// Установка продолжительности звука ошибочного теста (0.2 - 999.9).
      /// </summary>
      BUZZER_FTIME,

      /// <summary>
      /// Запрос ошибок из выходного буфера.
      /// </summary>
      ERROR,

      /// <summary>
      /// Запрос версии GPIB.
      /// </summary>
      GPIB_VERSION,
    }

    /// <summary>
    /// Словарь для получения синтаксиса команды по ее типу.
    /// </summary>
    private static readonly Dictionary<SystemCommand, string> CommandSyntax = new Dictionary<SystemCommand, string>
    {
      { SystemCommand.LCD_CONTRAST, "SYST:LCD:CONT" },
      { SystemCommand.LCD_BRIGHTNESS, "SYST:LCD:BRIG" },
      { SystemCommand.BUZZER_PSOUND, "SYST:BUZZ:PSOUND" },
      { SystemCommand.BUZZER_FSOUND, "SYST:BUZZ:FSOUND" },
      { SystemCommand.BUZZER_PTIME, "SYST:BUZZ:PTIME" },
      { SystemCommand.BUZZER_FTIME, "SYST:BUZZ:FTIME" },
      { SystemCommand.ERROR, "SYST:ERR" },
      { SystemCommand.GPIB_VERSION, "SYST:GPIB:VERSION" },
    };

    /// <summary>
    /// Получает строку команды по ключу из словаря.
    /// </summary>
    /// <param name="command">Ключ команды из перечисления SystemCommand.</param>
    /// <returns>Строка команды, соответствующая ключу.</returns>
    public static string GetCommandSyntax(SystemCommand command)
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
