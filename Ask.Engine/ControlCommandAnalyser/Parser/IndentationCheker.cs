
namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  public static class IndentationCheker
  {
    /// <summary>
    /// Проверяет корректность отступов в коде команд.
    /// </summary>
    /// <param name="lines">Массив строк с кодом.</param>
    /// <param name="commandNumber">Номер команды (например, "50").</param>
    /// <param name="mnemonic">Мнемоника (например, "ПР").</param>
    /// <returns>Список ошибок с номерами строк.</returns>
    public static List<string> CheckIndentationErrors(List<string> lines, string commandNumber, string mnemonic)
    {
      var errors = new List<string>();

      for (int i = 0; i < lines.Count; i++)
      {
        var line = lines[i];

        // Строка, с которой должна начинаться команда (например, "50 ПР")
        var expectedStart = $"{commandNumber} {mnemonic}";

        // 1. Если строка начинается с команды, но с отступом — ошибка
        if (line.TrimStart().StartsWith(expectedStart) && line.Length > 0 && char.IsWhiteSpace(line[0]))
        {
          errors.Add($"Строка {i + 1}: недопустимый отступ перед номером команды.");
          continue;
        }

        // 2. Если строка — не первая (i > 0) и содержит что-то, но не начинается с пробела или табуляции — ошибка
        if (i > 0 && !string.IsNullOrWhiteSpace(line) && !char.IsWhiteSpace(line[0]))
        {
          errors.Add($"Строка {i + 1}: отсутствует отступ в строке продолжения.");
        }
      }

      return errors;
    }
  }
}
