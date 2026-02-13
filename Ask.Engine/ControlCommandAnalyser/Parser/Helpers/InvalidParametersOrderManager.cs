namespace Ask.Engine.ControlCommandAnalyser.Parser.Helpers
{
  public static class InvalidParametersOrderManager
  {
    public static bool HasInvalidParameterOrder(
    string firstLine,
    List<string> algorithmKeys,
    string? capacityStart,
    out string errorDescription)
    {
      errorDescription = string.Empty;

      int idxKey = -1;
      int idxCapacity = -1;

      int idxPointStart = firstLine.IndexOf('*');
      int idxPointEnd = firstLine.LastIndexOf('*');

      // ищем самый ранний ключ
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

      // ключ после параметра
      if (idxKey != -1 && idxCapacity != -1 && idxKey > idxCapacity)
      {
        errorDescription = "Ключ алгоритма указан после электрической емкости.";
        return true;
      }

      // параметры после блока точек
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
