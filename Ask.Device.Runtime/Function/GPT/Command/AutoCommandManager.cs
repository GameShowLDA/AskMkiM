namespace Ask.Device.Runtime.Function.GPT.Command
{
  /// <summary>
  /// Класс для управления автоматическими командами.
  /// </summary>
  internal class AutoCommandManager
  {
    /// <summary>
    /// Перечисление автоматических команд устройства.
    /// </summary>
    public enum AutoCommand
    {
      /// <summary>
      /// Возврат страницы просмотра выбранного автоматического теста (от 1 до 100.)
      /// </summary>
      AUTO_PAGE_SHOW,

      /// <summary>
      /// Перемещение данных на дисплее с заданием шага (от 1 до 100).
      /// </summary>
      AUTO_PAGE_MOVE,

      /// <summary>
      /// Перестановка данных на дисплее с заданием шага
      /// <Vaue1> <NR1> 1~16 (source step)
      /// <Value2> <NR1> 1~16 (destination step)
      /// </summary>
      AUTO_PAGE_SWAP,

      /// <summary>
      /// Пропуск выбранных шагов, когда запущен автоматический тест (от 1 до 16 (step no.#), ON или OFF).
      /// </summary>
      AUTO_PAGE_SKIP,

      /// <summary>
      /// Удаление выбранных шагов из автоматического теста (от 1 до 16 (step no.#))
      /// </summary>
      AUTO_PAGE_DEL,

      /// <summary>
      /// Установка/возврат названия выбранного автоматического теста.
      /// </summary>
      AUTO_NAME,

      /// <summary>
      /// Добавление выбранного ручного теста в текущий автоматический номер  (от 1 до 100).
      /// </summary>
      AUTO_EDIT_ADD,

      /// <summary>
      /// Высвечивание "OK" в удаленном терминале, когда тест остановлен/идет/ошибка (ON или OFF).
      /// </summary>
      TESTOK_RETURN,
    }

    /// <summary>
    /// Словарь для получения синтаксиса команды по ее типу.
    /// </summary>
    public static readonly Dictionary<AutoCommand, string> CommandSyntax = new Dictionary<AutoCommand, string>
    {
        { AutoCommand.AUTO_PAGE_SHOW, "AUTO:PAGE:SHOW" }, // <x> может быть параметром, который вы добавите при использовании
        { AutoCommand.AUTO_PAGE_MOVE, "AUTO:PAGE:MOVE" },
        { AutoCommand.AUTO_PAGE_SWAP, "AUTO:PAGE:SWAP" },
        { AutoCommand.AUTO_PAGE_SKIP, "AUTO:PAGE:SKIP" },
        { AutoCommand.AUTO_PAGE_DEL, "AUTO:PAGE:DEL" },
        { AutoCommand.AUTO_NAME, "AUTO:NAME" },
        { AutoCommand.AUTO_EDIT_ADD, "AUTO:EDIT:ADD" },
        { AutoCommand.TESTOK_RETURN, "TEST:RET" },
    };

    /// <summary>
    /// Получает строку команды по ключу из словаря.
    /// </summary>
    /// <param name="command">Ключ команды из перечисления AutoCommand.</param>
    /// <returns>Строка команды, соответствующая ключу.</returns>
    public static string GetCommandSyntax(AutoCommand command)
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
