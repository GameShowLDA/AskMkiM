namespace Core.Communication
{
  /// <summary>
  /// Представляет команду, состоящую из номера и трех параметров.
  /// </summary>
  public class Command
  {
    /// <summary>
    /// Номер команды.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Первый параметр команды.
    /// </summary>
    public int FirstParameter { get; set; }

    /// <summary>
    /// Второй параметр команды.
    /// </summary>
    public int SecondParameter { get; set; }

    /// <summary>
    /// Третий параметр команды.
    /// </summary>
    public int ThirdParameter { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Command"/> с параметрами по умолчанию.
    /// </summary>
    public Command()
    {
      this.Number = 0;
      this.FirstParameter = 0;
      this.SecondParameter = 0;
      this.ThirdParameter = 0;
    }

    /// <summary>
    /// Возвращает строковое представление команды в формате "Number.FirstParameter.SecondParameter.ThirdParameter.".
    /// </summary>
    /// <returns>Строковое представление команды.</returns>
    public override string ToString()
    {
      return $"{Number}.{FirstParameter}.{SecondParameter}.{ThirdParameter}.";
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Command"/> из строкового представления.
    /// Ожидается формат строки "x.x.x.x.", где каждая часть представляет целое число.
    /// </summary>
    /// <param name="commandString">Строковое представление команды.</param>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если строка не содержит ровно 4 части, разделенные точками.
    /// </exception>
    /// <exception cref="FormatException">
    /// Выбрасывается, если какая-либо из частей не может быть преобразована в целое число.
    /// </exception>
    public Command(string commandString)
    {
      var parts = commandString.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

      if (parts.Length != 4)
      {
        throw new ArgumentException("Неверный формат строки. Ожидается формат 'x.x.x.x.' (4 части).");
      }

      int[] parsedParts = new int[4];
      for (int i = 0; i < parts.Length; i++)
      {
        if (!int.TryParse(parts[i], out parsedParts[i]))
        {
          throw new FormatException($"Часть '{parts[i]}' не является допустимым целым числом.");
        }
      }

      this.Number = parsedParts[0];
      this.FirstParameter = parsedParts[1];
      this.SecondParameter = parsedParts[2];
      this.ThirdParameter = parsedParts[3];
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Command"/> с заданным номером.
    /// Остальные параметры устанавливаются в 0.
    /// </summary>
    /// <param name="number">Номер команды.</param>
    public Command(int number) : this()
    {
      this.Number = number;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Command"/> с заданным номером и первым параметром.
    /// Остальные параметры устанавливаются в 0.
    /// </summary>
    /// <param name="number">Номер команды.</param>
    /// <param name="firstParameter">Первый параметр команды.</param>
    public Command(int number, int firstParameter) : this(number)
    {
      this.FirstParameter = firstParameter;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Command"/> с заданным номером, первым и вторым параметрами.
    /// Третий параметр устанавливается в 0.
    /// </summary>
    /// <param name="number">Номер команды.</param>
    /// <param name="firstParameter">Первый параметр команды.</param>
    /// <param name="secondParameter">Второй параметр команды.</param>
    public Command(int number, int firstParameter, int secondParameter) : this(number, firstParameter)
    {
      SecondParameter = secondParameter;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Command"/> с заданным номером, первым, вторым и третьим параметрами.
    /// </summary>
    /// <param name="number">Номер команды.</param>
    /// <param name="firstParameter">Первый параметр команды.</param>
    /// <param name="secondParameter">Второй параметр команды.</param>
    /// <param name="thirdParameter">Третий параметр команды.</param>
    public Command(int number, int firstParameter, int secondParameter, int thirdParameter) : this(number, firstParameter, secondParameter)
    {
      ThirdParameter = thirdParameter;
    }
  }
}
