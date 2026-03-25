namespace Ask.Device.Runtime.Commands
{
  /// <summary>
  /// Представляет команду устройства с номером и тремя параметрами.
  /// </summary>
  public class DeviceCommand
  {
    /// <summary>
    /// Получает или задаёт номер команды.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Получает или задаёт первый параметр команды.
    /// </summary>
    public int FirstParameter { get; set; }

    /// <summary>
    /// Получает или задаёт второй параметр команды.
    /// </summary>
    public int SecondParameter { get; set; }

    /// <summary>
    /// Получает или задаёт третий параметр команды.
    /// </summary>
    public int ThirdParameter { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceCommand"/> со значениями параметров по умолчанию.
    /// </summary>
    public DeviceCommand()
    {
      Number = 0;
      FirstParameter = 0;
      SecondParameter = 0;
      ThirdParameter = 0;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceCommand"/> из строкового представления.
    /// </summary>
    /// <param name="commandString">Строка в формате <c>x.x.x.x.</c>.</param>
    public DeviceCommand(string commandString)
    {
      if (string.IsNullOrWhiteSpace(commandString))
      {
        throw new ArgumentException("Строка команды не может быть пустой или содержать только пробелы.");
      }

      var parts = commandString.TrimEnd('.').Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length != 4)
      {
        throw new ArgumentException("Неверный формат строки. Ожидается формат 'x.x.x.x.' (4 части).");
      }

      int[] parsedParts = new int[4];
      for (int i = 0; i < parts.Length; i++)
      {
        if (!int.TryParse(parts[i], out parsedParts[i]) || parsedParts[i] < 0)
        {
          throw new FormatException($"Часть '{parts[i]}' не является допустимым неотрицательным целым числом.");
        }
      }

      Number = parsedParts[0];
      FirstParameter = parsedParts[1];
      SecondParameter = parsedParts[2];
      ThirdParameter = parsedParts[3];
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceCommand"/> с номером команды.
    /// </summary>
    /// <param name="number">Номер команды.</param>
    public DeviceCommand(int number)
      : this()
    {
      Number = number;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceCommand"/> с номером команды и первым параметром.
    /// </summary>
    /// <param name="number">Номер команды.</param>
    /// <param name="firstParameter">Первый параметр команды.</param>
    public DeviceCommand(int number, int firstParameter)
      : this(number)
    {
      FirstParameter = firstParameter;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceCommand"/> с номером команды и двумя параметрами.
    /// </summary>
    /// <param name="number">Номер команды.</param>
    /// <param name="firstParameter">Первый параметр команды.</param>
    /// <param name="secondParameter">Второй параметр команды.</param>
    public DeviceCommand(int number, int firstParameter, int secondParameter)
      : this(number, firstParameter)
    {
      SecondParameter = secondParameter;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="DeviceCommand"/> с номером команды и тремя параметрами.
    /// </summary>
    /// <param name="number">Номер команды.</param>
    /// <param name="firstParameter">Первый параметр команды.</param>
    /// <param name="secondParameter">Второй параметр команды.</param>
    /// <param name="thirdParameter">Третий параметр команды.</param>
    public DeviceCommand(int number, int firstParameter, int secondParameter, int thirdParameter)
      : this(number, firstParameter, secondParameter)
    {
      ThirdParameter = thirdParameter;
    }

    /// <summary>
    /// Преобразует команду в строку транспорта.
    /// </summary>
    /// <returns>Строка команды в формате <c>x.x.x.x.</c>.</returns>
    public override string ToString()
    {
      return $"{Number}.{FirstParameter}.{SecondParameter}.{ThirdParameter}.";
    }
  }
}
