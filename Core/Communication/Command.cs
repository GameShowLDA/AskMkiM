namespace Core.Communication
{
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

    public Command()
    {
      this.Number = 0;
      this.FirstParameter = 0;
      this.SecondParameter = 0;
      this.ThirdParameter = 0;
    }

    public override string ToString()
    {
      return $"{Number}.{FirstParameter}.{SecondParameter}.{ThirdParameter}.";
    }

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


    public Command(int number) : this()
    {
      this.Number = number;
    }

    public Command(int number, int firstParameter) : this(number)
    {
      this.FirstParameter = firstParameter;
    }

    public Command(int number, int firstParameter, int secondParameter) : this(number, firstParameter)
    {
      SecondParameter = secondParameter;
    }

    public Command(int number, int firstParameter, int secondParameter, int thirdParameter) : this(number, firstParameter, secondParameter)
    {
      ThirdParameter = thirdParameter;
    }
  }
}
