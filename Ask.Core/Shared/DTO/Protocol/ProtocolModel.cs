namespace Ask.Core.Shared.DTO.Protocol
{
  public class ProtocolModel
  {
    /// <summary>
    /// Дата протокола.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Обозначение сборочной единицы.
    /// </summary>
    public string Designation { get; set; }

    /// <summary>
    /// Наименование объекта контроля.
    /// </summary>
    public string ControlObjectName { get; set; }


    /// <summary>
    /// Номер сборочной единицы.
    /// </summary>
    public string Number { get; set; }

    /// <summary>
    /// Исполнитель.
    /// </summary>
    public string Executor { get; set; }

    /// <summary>
    /// Путь к программе контроля.
    /// </summary>
    public string ProgramPath { get; set; }

    /// <summary>
    /// Название программы контроля.
    /// </summary>
    public string ProgramName { get; set; }

    /// <summary>
    /// Представитель ОК.
    /// </summary>
    public string Agent { get; set; }

    /// <summary>
    /// Представитель заказчика(ВП).
    /// </summary>
    public string Customer { get; set; }

    /// <summary>
    /// Режим выполнения.
    /// </summary>
    public string Mode { get; set; }

    /// <summary>
    /// Список ошибок программы.
    /// </summary>
    public Dictionary<string, List<ShowMessageModel>> Errors { get; set; } = new Dictionary<string, List<ShowMessageModel>>();

    /// <summary>
    /// Время начала выполнения.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Время конца исполнения.
    /// </summary>
    public DateTime EndTime
    {
      get;
      set;
    }

    /// <summary>
    /// Общее время выполнения.
    /// </summary>
    public TimeSpan ExecutionTime
    {
      get
      {
        return EndTime - StartTime;
      }
    }

    private static string Template { get; set; } = string.Empty;
    private static string ErrorsTemplate { get; set; } = string.Empty;

    public ProtocolModel()
    {
      StartTime = DateTime.Now;
    }

    static public void SetTemplate(string templatePath)
    {
      Template = templatePath;
    }

    static public void SetErrorsTemplate(string templatePath)
    {
      ErrorsTemplate = templatePath;
    }


    static public string GetProtocolText(ProtocolModel protocolModel)
    {
      // Формируем финальный текст протокола
      string formattedText = Template
          .Replace("$ДАТА", protocolModel.Date.ToString("dd.MM.yyyy"))
          .Replace("$ОБОЗНАЧЕНИЕ", protocolModel.Designation)
          .Replace("$РЕЖИМ", protocolModel.Mode)
          .Replace("$НОМЕР", protocolModel.Number.ToString())
          .Replace("$ПРОГРАММА", protocolModel.ProgramName)
          .Replace("$НАЧАЛО", protocolModel.StartTime.ToString("HH:mm:ss:ff"))
          .Replace("$КОНЕЦ", protocolModel.EndTime.ToString("HH:mm:ss:ff"))
          .Replace("$ВРЕМЯ", protocolModel.ExecutionTime.ToString(@"hh\:mm\:ss\:ff"))
          .Replace("$ИСПОЛНИТЕЛЬ", protocolModel.Executor)
          .Replace("$ПРЕДСТАВИТЕЛЬ", protocolModel.Agent)
          .Replace("$ЗАКАЗЧИК", protocolModel.Customer);
      return formattedText;
    }

    public static string GetProtocolWithErrorsText(ProtocolModel protocolModel)
    {
      // 1. Формируем список ошибок
      int totalErrors = protocolModel.Errors.Values.Sum(list => list.Count);
      var errorsText = $"\r\nОшибки программы (всего: {totalErrors}):";

      int i = 1;
      foreach (var item in protocolModel.Errors.Keys)
      {
        //errorsText += $"\r\n\tОшибки команды: {item}";
        foreach (var error in protocolModel.Errors[item])
        {
          errorsText += $"\r\nERR{i} {item}: {error}";
          i++;
        }
      }

      // 2. Разделяем шаблон на две части — до и после строки с "$ПРОГРАММА"
      const string marker = "$ПРОГРАММА";
      int markerIndex = ErrorsTemplate.IndexOf(marker);
      if (markerIndex == -1)
        throw new InvalidOperationException("В шаблоне не найден маркер $ПРОГРАММА.");

      string before = ErrorsTemplate.Substring(0, markerIndex + marker.Length);
      string after = ErrorsTemplate.Substring(markerIndex + marker.Length);

      // 3. Выполняем подстановку в обеих частях отдельно
      before = before
          .Replace("$ДАТА", protocolModel.Date.ToString("dd.MM.yyyy"))
          .Replace("$ОБОЗНАЧЕНИЕ", protocolModel.Designation)
          .Replace("$РЕЖИМ", protocolModel.Mode)
          .Replace("$НОМЕР", protocolModel.Number.ToString())
          .Replace("$ПРОГРАММА", protocolModel.ProgramName);

      after = after
          .Replace("$ОБОЗНАЧЕНИЕ", protocolModel.Designation)
          .Replace("$НАИМЕНОВАНИЕ", protocolModel.ControlObjectName)
          .Replace("$НОМЕР", protocolModel.Number.ToString())
          .Replace("$БРАК(не )", "не ")
          .Replace("$ИСПОЛНИТЕЛЬ", protocolModel.Executor)
          .Replace("$ПРЕДСТАВИТЕЛЬ", protocolModel.Agent)
          .Replace("$ЗАКАЗЧИК", protocolModel.Customer);

      // 4. Склеиваем финальный текст: до → ошибки → после
      string formattedText = before + "\r\n" + errorsText + "\r\n" + after;

      return formattedText;
    }
  }
}
