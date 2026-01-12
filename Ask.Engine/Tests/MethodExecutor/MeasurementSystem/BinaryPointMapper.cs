using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;

namespace Ask.Engine.Tests.MethodExecutor.MeasurementSystem
{
  /// <summary>
  /// Отвечает за преобразование точек в двоичное представление с переворотом порядка битов.
  /// </summary>
  public class BinaryPointMapper
  {
    private readonly IEnumerable<IRelaySwitchModule> _relayModules;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BinaryPointMapper"/>.
    /// </summary>
    /// <param name="relayModules">Список доступных модулей коммутации реле.</param>
    public BinaryPointMapper(IEnumerable<IRelaySwitchModule> relayModules)
    {
      _relayModules = relayModules ?? throw new ArgumentNullException(nameof(relayModules));
    }

    /// <summary>
    /// Возвращает количество разрядов в двоичном представлении наибольшего номера точки в диапазоне.
    /// </summary>
    /// <param name="startPoint">Начальная точка диапазона.</param>
    /// <param name="endPoint">Конечная точка диапазона.</param>
    /// <returns>Количество битов в представлении наибольшего номера точки.</returns>
    public int GetHighestPointBinaryDigits(PointModel startPoint, PointModel endPoint)
    {
      if (startPoint == null || endPoint == null)
      {
        throw new ArgumentNullException("Точки начала и конца диапазона не могут быть null.");
      }

      var minPoint = (startPoint.ModuleNumber < endPoint.ModuleNumber) ? startPoint : endPoint;
      var maxPoint = (startPoint.ModuleNumber < endPoint.ModuleNumber) ? endPoint : startPoint;

      int maxPointNumber = 0;

      if (minPoint.ModuleNumber == maxPoint.ModuleNumber)
      {
        maxPointNumber = Math.Max(minPoint.PointNumber, maxPoint.PointNumber);
      }
      else
      {
        maxPointNumber = Math.Max(maxPointNumber, GetMaxPointNumberInModule(minPoint.DeviceNumber, minPoint.ModuleNumber));

        for (int module = minPoint.ModuleNumber + 1; module < maxPoint.ModuleNumber; module++)
        {
          maxPointNumber = Math.Max(maxPointNumber, GetMaxPointNumberInModule(minPoint.DeviceNumber, module));
        }

        maxPointNumber = Math.Max(maxPointNumber, maxPoint.PointNumber);
      }

      return Convert.ToString(maxPointNumber, 2).Length;
    }

    /// <summary>
    /// Преобразует все точки в диапазоне в перевёрнутые двоичные строки фиксированной длины.
    /// </summary>
    /// <param name="first">Первая точка диапазона.</param>
    /// <param name="second">Вторая точка диапазона.</param>
    /// <param name="bitLength">Желаемая длина двоичной строки.</param>
    /// <returns>Список точек и соответствующих перевёрнутых бинарных строк.</returns>
    public List<(PointModel point, string reversedBinary)> ConvertToReversedBinaryRange(
        PointModel first,
        PointModel second,
        int bitLength)
    {
      if (bitLength <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(bitLength), "Длина двоичной строки должна быть больше 0.");
      }

      var (startPoint, endPoint) = OrderPoints(first, second);
      var result = new List<(PointModel, string)>();

      for (int module = startPoint.ModuleNumber; module <= endPoint.ModuleNumber; module++)
      {
        var (startNumber, endNumber) = GetPointRangeForModule(startPoint, endPoint, module);

        for (int i = startNumber; i <= endNumber; i++)
        {
          string reversedBinary = ConvertToReversedBinary(i, bitLength);

          result.Add((new PointModel
          {
            DeviceNumber = startPoint.DeviceNumber,
            ModuleNumber = module,
            PointNumber = i,
          }, reversedBinary));
        }
      }

      return result;
    }

    /// <summary>
    /// Упорядочивает точки по возрастанию номера модуля.
    /// </summary>
    /// <param name="first">Первая точка.</param>
    /// <param name="second">Вторая точка.</param>
    /// <returns>Кортеж с упорядоченными точками (start, end).</returns>
    private (PointModel start, PointModel end) OrderPoints(PointModel first, PointModel second)
    {
      return first.ModuleNumber <= second.ModuleNumber
          ? (first, second)
          : (second, first);
    }

    /// <summary>
    /// Возвращает диапазон номеров точек в пределах одного модуля.
    /// </summary>
    /// <param name="start">Начальная точка диапазона.</param>
    /// <param name="end">Конечная точка диапазона.</param>
    /// <param name="module">Номер модуля.</param>
    /// <returns>Кортеж с начальным и конечным номерами точек в модуле.</returns>
    private (int start, int end) GetPointRangeForModule(PointModel start, PointModel end, int module)
    {
      if (start.ModuleNumber == end.ModuleNumber)
      {
        return (start.PointNumber, end.PointNumber);
      }
      else if (module == start.ModuleNumber)
      {
        return (start.PointNumber, GetMaxPointNumberInModule(start.DeviceNumber, module));
      }
      else if (module == end.ModuleNumber)
      {
        return (1, end.PointNumber);
      }
      else
      {
        return (1, GetMaxPointNumberInModule(start.DeviceNumber, module));
      }
    }

    /// <summary>
    /// Получает максимальный номер точки для указанного модуля.
    /// </summary>
    /// <param name="deviceNumber">Номер устройства (шасси).</param>
    /// <param name="moduleNumber">Номер модуля.</param>
    /// <returns>Максимальное количество точек в модуле.</returns>
    private int GetMaxPointNumberInModule(int deviceNumber, int moduleNumber)
    {
      return _relayModules
          .FirstOrDefault(m => m.NumberChassis == deviceNumber && m.Number == moduleNumber)
          ?.PointCount ?? 0;
    }

    /// <summary>
    /// Преобразует число в двоичную строку заданной длины и переворачивает её.
    /// </summary>
    /// <param name="number">Число для преобразования.</param>
    /// <param name="bitLength">Желаемая длина строки.</param>
    /// <returns>Перевёрнутая двоичная строка.</returns>
    private string ConvertToReversedBinary(int number, int bitLength)
    {
      string binary = Convert.ToString(number, 2).PadLeft(bitLength, '0');
      char[] array = binary.ToCharArray();
      Array.Reverse(array);
      return new string(array);
    }
  }
}
