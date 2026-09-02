using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Metadata.Static
{
  /// <summary>
  /// Хранит последние корректные значения полей метрологических и модульных тестов.
  /// </summary>
  public static class MeasurementTestData
  {
    private static readonly object SyncRoot = new();
    private static PointModel? firstPoint;
    private static PointModel? secondPoint;
    private static double? electricalParameter;
    private static double? time;
    private static double? rampTime;
    private static double? voltage;
    private static BusPoint? bus;
    private static SwitchingBusNew? busGroup;
    private static string? testedNumber;
    private static string? testerNumber;
    private static string? testRange;

    /// <summary>
    /// Сохраняет проверенные параметры метрологического теста.
    /// </summary>
    /// <param name="dataModel">Проверенные параметры теста.</param>
    /// <param name="includeTime">Признак использования времени выполнения.</param>
    /// <param name="includeRampTime">Признак использования времени нарастания.</param>
    /// <param name="includeVoltage">Признак использования напряжения.</param>
    /// <param name="includeBus">Признак использования активной шины.</param>
    /// <param name="includeBusGroup">Признак использования группы шин.</param>
    public static void SaveMeasurementData(
      DataModel dataModel,
      bool includeTime = false,
      bool includeRampTime = false,
      bool includeVoltage = false,
      bool includeBus = false,
      bool includeBusGroup = false)
    {
      ArgumentNullException.ThrowIfNull(dataModel);
      ArgumentNullException.ThrowIfNull(dataModel.FirstPoint);
      ArgumentNullException.ThrowIfNull(dataModel.SecondPoint);

      lock (SyncRoot)
      {
        firstPoint = CopyPoint(dataModel.FirstPoint);
        secondPoint = CopyPoint(dataModel.SecondPoint);
        electricalParameter = dataModel.Param;

        if (includeTime)
          time = dataModel.Time;

        if (includeRampTime)
          rampTime = dataModel.RampTime;

        if (includeVoltage)
          voltage = dataModel.Voltage;

        if (includeBus)
          bus = dataModel.ActiveBus;

        if (includeBusGroup)
          busGroup = dataModel.ActivePairBus;
      }
    }

    /// <summary>
    /// Сохраняет проверенные параметры теста модулей коммутации реле.
    /// </summary>
    /// <param name="dataModel">Проверенные номера модулей и диапазон точек.</param>
    public static void SaveModuleTestData(DataModel dataModel)
    {
      ArgumentNullException.ThrowIfNull(dataModel);

      if (string.IsNullOrWhiteSpace(dataModel.TestedNumber))
        throw new ArgumentException("Не задан номер проверяемого модуля.", nameof(dataModel));

      if (string.IsNullOrWhiteSpace(dataModel.TesterNumber))
        throw new ArgumentException("Не задан номер проверяющего модуля.", nameof(dataModel));

      if (string.IsNullOrWhiteSpace(dataModel.TestRange))
        throw new ArgumentException("Не задан диапазон проверяемых точек.", nameof(dataModel));

      lock (SyncRoot)
      {
        testedNumber = dataModel.TestedNumber;
        testerNumber = dataModel.TesterNumber;
        testRange = dataModel.TestRange;
      }
    }

    /// <summary>
    /// Возвращает копию последних сохранённых значений полей ввода.
    /// </summary>
    /// <returns>Модель с последними корректными значениями полей.</returns>
    public static DataModel GetData()
    {
      lock (SyncRoot)
      {
        DataModel dataModel = new()
        {
          Param = electricalParameter ?? default,
          Time = time ?? default,
          RampTime = rampTime ?? default,
          Voltage = voltage ?? default,
          ActiveBus = bus ?? default,
          ActivePairBus = busGroup ?? default
        };

        if (firstPoint != null)
          dataModel.FirstPoint = CopyPoint(firstPoint);

        if (secondPoint != null)
          dataModel.SecondPoint = CopyPoint(secondPoint);

        if (testedNumber != null)
          dataModel.TestedNumber = testedNumber;

        if (testerNumber != null)
          dataModel.TesterNumber = testerNumber;

        if (testRange != null)
          dataModel.TestRange = testRange;

        return dataModel;
      }
    }

    private static PointModel CopyPoint(PointModel point) =>
      new()
      {
        DeviceNumber = point.DeviceNumber,
        ModuleNumber = point.ModuleNumber,
        PointNumber = point.PointNumber,
        PointType = point.PointType,
        Mnemonic = point.Mnemonic
      };
  }
}
