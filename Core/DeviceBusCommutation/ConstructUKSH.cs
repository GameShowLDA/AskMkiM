using static Core.DeviceBusCommutation.Functions;

namespace Core.DeviceBusCommutation
{
  /// <summary>
  /// Конструкция УКШ.
  /// </summary>
  public class ConstructUKSH
  {
    /// <summary>
    /// Gets получает словарь, сопоставляющий номера реле с их значениями LE.
    /// Ключ - номер реле (строка), значение - значение LE (строка).
    /// </summary>
    public Dictionary<string, string> ValueLE { get; private set; }

    /// <summary>
    /// Gets получает словарь, сопоставляющий номера реле с их битовыми индексами в M74HCT573.
    /// Ключ - номер реле (строка), значение - битовый индекс (целое число).
    /// </summary>
    public Dictionary<string, int> ValueM74HCT573 { get; private set; }

    /// <summary>
    /// Gets получает словарь, сопоставляющий значения LE с состоянием портов.
    /// Ключ - значение LE (строка), значение - состояние порта (целое число).
    /// </summary>
    public Dictionary<string, int> ValueStatePort { get; private set; }

    /// <summary>
    /// Gets получает словарь, сопоставляющий номера реле с их состоянием (замкнуто/разомкнуто).
    /// Ключ - номер реле (строка), значение - состояние точки (логическое значение).
    /// </summary>
    public Dictionary<string, bool> ValuePointState { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConstructUKSH"/> class.
    /// </summary>
    public ConstructUKSH()
    {
      this.ValueLE = GetValueLE;
      this.ValueM74HCT573 = GetValueM74HCT573UKSH;
      this.ValueStatePort = GetValueStatePortM74HCT573UKSH;
      this.ValuePointState = GetPointState;
    }
  }
}
