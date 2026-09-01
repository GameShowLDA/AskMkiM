using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Metadata.Static
{
  public static class MeasurementTestData
  {
    /// <summary>
    /// Первая точка.
    /// </summary>
    static public string FisrtPoint { get; set; }

    /// <summary>
    /// Вторая точка.
    /// </summary>
    static public string SecondPoint { get; set; }

    /// <summary>
    /// Получает или задаёт номер проверяемого устройства в формате a.b.
    /// </summary>
    static public string TestedNumber { get; set; }

    /// <summary>
    /// Получает или задаёт номер проверяющего устройства в формате a.b.
    /// </summary>
    static public string TesterNumber { get; set; }

    /// <summary>
    /// Получает или задаёт диапазон проверки в формате списка чисел и диапазонов (например, "1-3,5").
    /// </summary>
    static public string TestRange { get; set; }

    /// <summary>
    /// Электрический параметр.
    /// </summary>
    static public double ElectricalParameter { get; set; }

    /// <summary>
    /// Время выполнения теста.
    /// </summary>
    static public double TimeTest { get; set; }

    /// <summary>
    /// Время нарастания напряжения.
    /// </summary>
    static public double RampTime { get; set; }

    /// <summary>
    /// Напряжение.
    /// </summary>
    static public double Voltage { get; set; }

    /// <summary>
    /// Только активная шина.
    /// </summary>
    static public BusPoint Bus { get; set; }

    /// <summary>
    /// Активная группа шин (AB1..AB4).
    /// </summary>
    static public SwitchingBusNew BusGroup { get; set; }

    static public void SetData(DataModel dataModel)
    {
      SetFirstPoint(dataModel.FirstPoint.ToString());
      SetSecondPoint(dataModel.SecondPoint.ToString());
      SetElectricalParam(dataModel.Param);
      SetTestTime(dataModel.Time);
      SetRampTime(dataModel.RampTime);
      SetVoltage(dataModel.Voltage);
      SetActiveBus(dataModel.ActiveBus);
      SetActivePairBus(dataModel.ActivePairBus);
    }

    static private void SetFirstPoint(string point)
    {
      if (!string.IsNullOrEmpty(point))
      {
        FisrtPoint = point;
      }
    }

    static private void SetSecondPoint(string point)
    {
      if (!string.IsNullOrEmpty(point))
      {
        SecondPoint = point;
      }
    }

    static private void SetElectricalParam(double param)
    {
      if (param != 0)
      {
        ElectricalParameter = param;
      }
    }

    static private void SetTestTime(double time)
    {
      if (time != 0)
      {
        TimeTest = time;
      }
    }

    static private void SetRampTime(double time)
    {
      if (time != 0)
      {
        RampTime = time;
      }
    }

    static private void SetVoltage(double voltage)
    {
      if (voltage != 0)
      {
        Voltage = voltage;
      }
    }

    static private void SetActiveBus(BusPoint bus)
    {
      if (bus != default)
      {
        Bus = bus;
      }
    }

    static private void SetActivePairBus(SwitchingBusNew buses)
    {
      if (buses != default)
      {
        BusGroup = buses;
      }
    }
  }
}
