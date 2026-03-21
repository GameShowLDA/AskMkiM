using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Device.Communication.Ethernet.Udp
{
  /// <summary>
  /// Класс DeviceCommand представляет команду устройства с номером и тремя параметрами.
  /// </summary>
  public class DeviceCommand
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
    /// Инициализирует новый экземпляр класса DeviceCommand с начальными значениями всех параметров (0).
    /// </summary>
    public DeviceCommand()
    {
      this.Number = 0;
      this.FirstParameter = 0;
      this.SecondParameter = 0;
      this.ThirdParameter = 0;
    }

    /// <summary>
    /// Преобразует объект DeviceCommand в строковое представление.
    /// Строковое представление имеет формат 'x.x.x.x.', где x — числовое значение параметра.
    /// </summary>
    /// <returns>Строковое представление команды.</returns>
    public override string ToString()
    {
      return $"{Number}.{FirstParameter}.{SecondParameter}.{ThirdParameter}.";
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса DeviceCommand из строки.
    /// Строка должна иметь формат 'x.x.x.x.', где x — числовое значение параметра.
    /// </summary>
    /// <param name="commandString">Строка, представляющая команду.</param>
    /// <exception cref="ArgumentException">Выбрасывается, если строка имеет неверный формат.</exception>
    /// <exception cref="FormatException">Выбрасывается, если одна из частей строки не является допустимым целым числом.</exception>
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

      this.Number = parsedParts[0];
      this.FirstParameter = parsedParts[1];
      this.SecondParameter = parsedParts[2];
      this.ThirdParameter = parsedParts[3];
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса DeviceCommand с указанным номером команды.
    /// Остальные параметры инициализируются значением по умолчанию (0).
    /// </summary>
    /// <param name="number">Номер команды.</param>
    public DeviceCommand(int number) : this()
    {
      this.Number = number;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса DeviceCommand с указанным номером команды и первым параметром.
    /// Остальные параметры инициализируются значением по умолчанию (0).
    /// </summary>
    /// <param name="number">Номер команды.</param>
    /// <param name="firstParameter">Первый параметр команды.</param>
    public DeviceCommand(int number, int firstParameter) : this(number)
    {
      this.FirstParameter = firstParameter;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса DeviceCommand с указанным номером команды, первым и вторым параметрами.
    /// Остальные параметры инициализируются значением по умолчанию (0).
    /// </summary>
    /// <param name="number">Номер команды.</param>
    /// <param name="firstParameter">Первый параметр команды.</param>
    /// <param name="secondParameter">Второй параметр команды.</param>
    public DeviceCommand(int number, int firstParameter, int secondParameter) : this(number, firstParameter)
    {
      this.SecondParameter = secondParameter;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса DeviceCommand с указанным номером команды, первым, вторым и третьим параметрами.
    /// </summary>
    /// <param name="number">Номер команды.</param>
    /// <param name="firstParameter">Первый параметр команды.</param>
    /// <param name="secondParameter">Второй параметр команды.</param>
    /// <param name="thirdParameter">Третий параметр команды.</param>
    public DeviceCommand(int number, int firstParameter, int secondParameter, int thirdParameter) : this(number, firstParameter, secondParameter)
    {
      this.ThirdParameter = thirdParameter;
    }
  }
}
