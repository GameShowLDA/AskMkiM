namespace Ask.Device.Runtime.Function.GPT.Command
{
  /// <summary>
  /// Класс для управления ручными командами.
  /// </summary>
  static internal class ManualCommandManager
  {
    /// <summary>
    /// Перечисление ручных команд устройства.
    /// </summary>
    public enum ManualCommand
    {
      /// <summary>
      /// Установка номера ручного теста (от 0 до 100).
      /// </summary>
      MANU_STEP,

      /// <summary>
      /// Установка/возврат названия теста в выбранном ручном тесте (строка длинной в 10 символов, где первый символ буква).
      /// </summary>
      MANU_NAME,

      /// <summary>
      /// Установка/возврат времени нарастания (0,1 - 999,9).
      /// </summary>
      MANU_RTIME,

      /// <summary>
      /// Установка/возврат выбранного режима теста (ACW, DCW, IR, GB).
      /// </summary>
      MANU_EDIT_MODE,

      /// <summary>
      /// Установка/возврат напряжения для ACW (от 0.100  до 5.000 кВ).
      /// </summary>
      MANU_ACW_VOLTAGE,

      /// <summary>
      /// Установка/возврат текущего высокого значения тока (верхний предел) для ACW (от 0.001 до 110.0).
      /// </summary>
      MANU_ACW_CHISET,

      /// <summary>
      /// Установка/возврат текущего низкого значения тока (нижний предел) для ACW (от 0.000 до 109.9).
      /// </summary>
      MANU_ACW_CLOSET,

      /// <summary>
      /// Установка/возврат времени теста в секундах для ACW (от 0.5 до 999.9 секунд, OFF)
      /// </summary>
      MANU_ACW_TTIME,

      /// <summary>
      /// Установка/возврат частоты в Гц для ACW ( 50Гц, 60Гц).
      /// </summary>
      MANU_ACW_FREQUENCY,

      /// <summary>
      /// Смещение для ACW ( от 0.000 до 109.9).
      /// </summary>
      MANU_ACW_REF,

      /// <summary>
      /// Установка/возврат текущего значения тока для ACW (от 2.000 до 200.0).
      /// </summary>
      MANU_ACW_ARCCURRENT,

      /// <summary>
      /// Установка/возврат напряжения для DCW (от 0.100 до 6.100 kV).
      /// </summary>
      MANU_DCW_VOLTAGE,

      /// <summary>
      /// Установка/возврат текущего высокого значения тока (верхний предел) для DCW (от 0.001 до 021.0).
      /// </summary>
      MANU_DCW_CHISET,

      /// <summary>
      /// Установка/возврат текущего низкого значения тока (нижний предел) для DCW (от 0.000 до 020.9).
      /// </summary>
      MANU_DCW_CLOSET,

      /// <summary>
      /// Установка/возврат времени теста в секундах для DCW (от 0.5 до 999.9, OFF).
      /// </summary>
      MANU_DCW_TTIME,

      /// <summary>
      /// Смещение для DCW (от 0.000 до 020.9).
      /// </summary>
      MANU_DCW_REF,

      /// <summary>
      /// Установка/возврат текущего значения тока для DCW (от 2.000 до 040.0).
      /// </summary>
      MANU_DCW_ARCCURRENT,

      /// <summary>
      /// Установка/возврат напряжения для IR (от 0.05 до 1 кВ с шагом 0.125).
      /// </summary>
      MANU_IR_VOLTAGE,

      /// <summary>
      /// Установка/возврат текущего высокого значения сопротивления (верхний предел) для IR
      /// Format A: 0.002 ~ 50.00 (unit = GΩ)
      /// Format B: 0.002G ~ 50.00G
      /// Format C: 2M ~ 50000M
      /// </summary>
      MANU_IR_RHISET,

      /// <summary>
      /// Установка/возврат текущего низкого значения сопротивления (нижний предел) для IR.
      /// Format A: 0.001 ~ 50.00 (unit = GΩ)
      /// Format B: 0.001G ~ 50.00G
      /// Format C: 1M ~ 50000M
      /// </summary>
      MANU_IR_RLOSET,

      /// <summary>
      /// Установка/возврат времени теста в секундах для IR (от 1.0 до 999.9 секунд).
      /// </summary>
      MANU_IR_TTIME,

      /// <summary>
      /// Смещение для IR.
      /// Format A: 0 ~ 50.00 (unit = GΩ)
      /// Format B: 0G ~ 50.00G
      /// Format C: 0M ~ 50000M
      /// </summary>
      MANU_IR_REF,

      /// <summary>
      /// Установка/возврат тока для GB (от 3.00 до 33.00)
      /// </summary>
      MANU_GB_CURRENT,

      /// <summary>
      /// Установка/возврат текущего высокого значения сопротивления (верхний предел) для GB (от 000.1 до 650.0).
      /// </summary>
      MANU_GB_RHISET,

      /// <summary>
      /// Установка/возврат текущего низкого значения сопротивления (нижний предел) для GB (от 0.000 до 649.9).
      /// </summary>
      MANU_GB_RLOSET,

      /// <summary>
      /// Установка/возврат времени теста в секундах для GB (от 0.5 до 999.9).
      /// </summary>
      MANU_GB_TTIME,

      /// <summary>
      /// Установка/возврат частоты в Гц для GB (50 или 60 Гц).
      /// </summary>
      MANU_GB_FREQUENCY,

      /// <summary>
      /// Смещение для GB (от 0.000 до 649.9).
      /// </summary>
      MANU_GB_REF,

      /// <summary>
      /// Выполняет функцию проверки на 0 для GB (ON или OFF).
      /// </summary>
      MANU_GB_ZEROCHECK,

      /// <summary>
      /// Установка/возврат статуса ARC режима текущего теста (OFF или ON_CONT или ON_STOP).
      /// </summary>
      MANU_UTILITY_ARCMODE,

      /// <summary>
      /// Установка/возврат настройки PASS SHOULD для текущего теста (ON или OFF).
      /// </summary>
      MANU_UTILITY_PASSHOULD,

      /// <summary>
      /// Установка/возврат настройки FAIL режима для текущего теста (CONT или HOLD или STOP).
      /// </summary>
      MANU_UTILITY_FAILMODE,

      /// <summary>
      /// Установка/возврат настройки MAX HOLD для текущего теста (ON или OFF).
      /// </summary>
      MANU_UTILITY_MAXHOLD,

      /// <summary>
      /// Установка/возврат подтягивающего режима для текущего теста (ON или OFF).
      /// </summary>
      MANU_UTILITY_GROUNDMODE,

      /// <summary>
      /// Установка/возврат параметров теста для справочных тестов (от 000 до 100).
      /// </summary>
      MANU_EDIT_SHOW,
    }

    /// <summary>
    /// Словарь для получения синтаксиса команды по ее типу.
    /// </summary>
    public static readonly Dictionary<ManualCommand, string> CommandSyntax = new Dictionary<ManualCommand, string>
    {
        { ManualCommand.MANU_STEP, "MANU:STEP" },
        { ManualCommand.MANU_NAME, "MANU:NAME" },
        { ManualCommand.MANU_RTIME, "MANU:RTIM" },
        { ManualCommand.MANU_EDIT_MODE, "MANU:EDIT:MODE" },
        { ManualCommand.MANU_ACW_VOLTAGE, "MANU:ACW:VOLT" },
        { ManualCommand.MANU_ACW_CHISET, "MANU:ACW:CHIS" },
        { ManualCommand.MANU_ACW_CLOSET, "MANU:ACW:CLOS" },
        { ManualCommand.MANU_ACW_TTIME, "MANU:ACW:TTIM" },
        { ManualCommand.MANU_ACW_FREQUENCY, "MANU:ACW:FREQ" },
        { ManualCommand.MANU_ACW_REF, "MANU:ACW:REF" },
        { ManualCommand.MANU_ACW_ARCCURRENT, "MANU:ACW:ARCC" },
        { ManualCommand.MANU_DCW_VOLTAGE, "MANU:DCW:VOLT" },
        { ManualCommand.MANU_DCW_CHISET, "MANU:DCW:CHIS" },
        { ManualCommand.MANU_DCW_CLOSET, "MANU:DCW:CLOS" },
        { ManualCommand.MANU_DCW_TTIME, "MANU:DCW:TTIM" },
        { ManualCommand.MANU_DCW_REF, "MANU:DCW:REF" },
        { ManualCommand.MANU_DCW_ARCCURRENT, "MANU:DCW:ARCC" },
        { ManualCommand.MANU_IR_VOLTAGE, "MANU:IR:VOLT" },
        { ManualCommand.MANU_IR_RHISET, "MANU:IR:RHIS" },
        { ManualCommand.MANU_IR_RLOSET, "MANU:IR:RLOS" },
        { ManualCommand.MANU_IR_TTIME, "MANU:IR:TTIM" },
        { ManualCommand.MANU_IR_REF, "MANU:IR:REF" },
        { ManualCommand.MANU_GB_CURRENT, "MANU:GB:CURR" },
        { ManualCommand.MANU_GB_RHISET, "MANU:GB:RHIS" },
        { ManualCommand.MANU_GB_RLOSET, "MANU:GB:RLOS" },
        { ManualCommand.MANU_GB_TTIME, "MANU:GB:TTIM" },
        { ManualCommand.MANU_GB_FREQUENCY, "MANU:GB:FREQ" },
        { ManualCommand.MANU_GB_REF, "MANU:GB:REF" },
        { ManualCommand.MANU_GB_ZEROCHECK, "MANU:GB:ZEROCHECK" },
        { ManualCommand.MANU_UTILITY_ARCMODE, "MANU:UTIL:ARCM" },
        { ManualCommand.MANU_UTILITY_PASSHOULD, "MANU:UTIL:PASS" },
        { ManualCommand.MANU_UTILITY_FAILMODE, "MANU:UTIL:FAIL" },
        { ManualCommand.MANU_UTILITY_MAXHOLD, "MANU:UTIL:MAXH" },
        { ManualCommand.MANU_UTILITY_GROUNDMODE, "MANU:UTIL:GROUNDMODE" },
        { ManualCommand.MANU_EDIT_SHOW, "MANU:EDIT:SHOW" },
    };

    /// <summary>
    /// Получает строку команды по ключу из словаря.
    /// </summary>
    /// <param name="command">Ключ команды из перечисления ManualCommand.</param>
    /// <returns>Строка команды, соответствующая ключу.</returns>
    public static string GetCommandSyntax(ManualCommand command)
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
