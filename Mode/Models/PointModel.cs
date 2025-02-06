namespace Mode.Models
{
  /// <summary>
  /// Модель точки.
  /// </summary>
  public class PointModel
  {
    /// <summary>
    /// Gets or sets номер устройства.
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
  }
}
