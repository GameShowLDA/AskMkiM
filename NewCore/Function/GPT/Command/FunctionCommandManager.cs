namespace NewCore.Function.GPT.Command
{
  /// <summary>
  /// Функциональные команды ППУ.
  /// </summary>
  static internal class FunctionCommandManager
  {
    /// <summary>
    /// Перечисление функциональных команд устройства.
    /// </summary>
    public enum FunctionCommand
    {
      /// <summary>
      /// Включение/выключение выбранного теста (ON , OFF).
      /// </summary>
      FUNCTION_TEST,

      /// <summary>
      /// Возврат параметров и результатов теста (от 1 до 16).
      /// </summary>
      MEASURE,

      /// <summary>
      /// Выбор между Автоматическим и Ручным режимом (AUTO , MANU).
      /// </summary>
      MAIN_FUNCTION,
    }

    /// <summary>
    /// Словарь для получения синтаксиса команды по ее типу.
    /// </summary>
    public static readonly Dictionary<FunctionCommand, string> CommandSyntax = new Dictionary<FunctionCommand, string>
      {
        { FunctionCommand.FUNCTION_TEST, "FUNC:TEST" },
        { FunctionCommand.MEASURE, "MEAS" },
        { FunctionCommand.MAIN_FUNCTION, "MAIN:FUNC" },
      };

    /// <summary>
    /// Получает строку команды по ключу из словаря.
    /// </summary>
    /// <param name="command">Ключ команды из перечисления FunctionCommand.</param>
    /// <returns>Строка команды, соответствующая ключу.</returns>
    public static string GetCommandSyntax(FunctionCommand command)
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
