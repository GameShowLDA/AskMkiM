using Ask.Core.Shared.Metadata.Enums.TranslationEnums;

namespace Ask.Core.Shared.DTO.Devices.RelaySwitchModule
{
  /// <summary>
  /// Модель точки.
  /// </summary>
  public class PointModel
  {
    /// <summary>
    /// Gets or sets номер шасси.
    /// </summary>
    public int DeviceNumber { get; set; }

    /// <summary>
    /// Gets or sets номер модуля.
    /// </summary>
    public int ModuleNumber { get; set; }

    /// <summary>
    /// Gets or sets номер точки.
    /// </summary>
    public int PointNumber { get; set; }

    public PointType PointType { get; set; }
    public string Mnemonic { get; set; }

    /// <summary>
    /// Метод для поиска строки формата "x.x.x" и возвращения объекта PointModel.
    /// </summary>
    /// <param name="input">Строка формата "x.x.x".</param>
    /// <returns>Возвращает модель точки.</returns>
    public static PointModel ParsePointString(string input)
    {
      string[] parts = input.Split('.');
      if (parts.Length != 3)
      {
        return null;
      }

      PointModel pointModel = new PointModel();

      if (!int.TryParse(parts[0], out int deviceNumber) ||
          !int.TryParse(parts[1], out int moduleNumber) ||
          !int.TryParse(parts[2], out int pointNumber))
      {
        return null;
      }

      pointModel.DeviceNumber = deviceNumber;
      pointModel.ModuleNumber = moduleNumber;
      pointModel.PointNumber = pointNumber;

      return pointModel;
    }

    /// <summary>
    /// Проверка второй точки на уникальность.
    /// </summary>
    /// <param name="secondPoint"></param>
    /// <returns></returns>
    public bool ValidateUnique(PointModel secondPoint)
    {
      if (DeviceNumber == secondPoint.DeviceNumber
        && ModuleNumber == secondPoint.ModuleNumber
        && PointNumber == secondPoint.PointNumber)
      {
        return false;
      }
      else
      {
        return true;
      }
    }

    /// <summary>
    /// Преобразует объект PointModel в строковое представление.
    /// Строковое представление имеет формат 'x.x.x'.
    /// </summary>
    /// <returns>Строковое представление команды.</returns>
    public override string ToString()
    {
      return $"{DeviceNumber}.{ModuleNumber}.{PointNumber}";
    }

    /// <summary>
    /// Преобразует список строк формата "x.x.x" в список моделей <see cref="PointModel"/>.
    /// Некорректные строки будут проигнорированы.
    /// </summary>
    /// <param name="pointStrings">Список строк точек.</param>
    /// <returns>Список <see cref="PointModel"/>.</returns>
    public static List<PointModel> ConvertToPointModels(List<string> pointStrings)
    {
      //var points = new List<PointModel>();
      return pointStrings
          .Select(PointModel.ParsePointString)
          .Where(p => p != null)
          .ToList();
    }

    /// <summary>
    /// Преобразует список моделей <see cref="PointModel"/> в список строк формата "x.x.x".
    /// </summary>
    /// <param name="points">Список моделей точек.</param>
    /// <returns>Список строковых представлений точек.</returns>
    public static List<string> ConvertToPointStrings(List<PointModel> points)
    {
      return points?
        .Select(p => p.ToString())
        .ToList()
        ?? new List<string>();
    }

    public override bool Equals(object? obj)
    {
      if (obj is not PointModel other)
        return false;

      return DeviceNumber == other.DeviceNumber
          && ModuleNumber == other.ModuleNumber
          && PointNumber == other.PointNumber;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(DeviceNumber, ModuleNumber, PointNumber);
    }
  }
}
