namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Утилитный класс для проверки корректности порядка параметров в строке команды.
  /// </summary>
  public static class InvalidParametersOrderManager
  {
    /// <summary>
    /// Определяет, нарушен ли порядок параметров (ключ алгоритма, ёмкость, точки).
    /// </summary>
    /// <param name="firstLine">Первая строка команды.</param>
    /// <param name="algorithmKeys">Список ключей алгоритма.</param>
    /// <param name="capacityStart">Начало параметра ёмкости.</param>
    /// <param name="errorDescription">Описание ошибки, если порядок нарушен.</param>
    /// <returns>
    /// <c>true</c>, если обнаружено нарушение порядка; иначе <c>false</c>.
    /// </returns>
    public static bool HasInvalidParameterOrder(string firstLine, List<string> algorithmKeys, string? capacityStart, out string errorDescription)
    {
      errorDescription = string.Empty;

      int idxKey = -1;
      int idxCapacity = -1;

      int idxPointStart = firstLine.IndexOf('*');
      int idxPointEnd = firstLine.LastIndexOf('*');

      foreach (var key in algorithmKeys)
      {
        int idx = firstLine.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && (idxKey == -1 || idx < idxKey))
          idxKey = idx;
      }

      if (!string.IsNullOrWhiteSpace(capacityStart))
      {
        idxCapacity = firstLine.IndexOf(capacityStart, StringComparison.OrdinalIgnoreCase);
      }

      if (idxKey != -1 && idxCapacity != -1 && idxKey > idxCapacity)
      {
        errorDescription = "Ключ алгоритма указан после электрической емкости.";
        return true;
      }

      if (idxPointEnd != -1)
      {
        if ((idxKey != -1 && idxKey > idxPointEnd)
         || (idxCapacity != -1 && idxCapacity > idxPointEnd))
        {
          errorDescription = "Один из параметров указан после точек.";
          return true;
        }
      }

      return false;
    }
  }
}
