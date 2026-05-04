
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

        var expectedStart = $"{commandNumber} {mnemonic}";

        if (line.TrimStart().StartsWith(expectedStart) && line.Length > 0 && char.IsWhiteSpace(line[0]))
        {
          errors.Add($"Строка {i + 1}: недопустимый отступ перед номером команды.");
          continue;
        }

        if (i > 0 && !string.IsNullOrWhiteSpace(line) && !char.IsWhiteSpace(line[0]) && !IsStandaloneChainSeparator(line))
        {
          errors.Add($"Строка {i + 1}: отсутствует отступ в строке продолжения.");
        }
      }

      return errors;
    }

    private static bool IsStandaloneChainSeparator(string line)
    {
      return line.Trim() == "*";
    }
  }
}
