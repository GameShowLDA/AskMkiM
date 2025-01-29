using Core.Abstract;

namespace Core.ConfigCollector
{
  /// <summary>
  /// Часть класса, представляющая установку различных значений конфигурации.
  /// </summary>
  static public partial class ConfigCollector
  {
    /// <summary>
    /// Устанавливает быстрый измеритель.
    /// </summary>
    /// <param name="deviceModel">Модель устройства быстрого измерителя.</param>
    static public void SetFastMeter(MeterBase deviceModel)
    {
      FastMeter = deviceModel;
    }

    /// <summary>
    /// Устанавливает быстрый измеритель.
    /// </summary>
    /// <param name="deviceModel">Модель устройства быстрого измерителя.</param>
    static public void SetBreakdown(BreakdownBase deviceModel)
    {
      Breakdown = deviceModel;
    }

    /// <summary>
    /// Устанавливает быстрый измеритель.
    /// </summary>
    /// <param name="deviceModel">Модель устройства быстрого измерителя.</param>
    static public void SetAccurateMeter(MeterBase deviceModel)
    {
      AccurateMeter = deviceModel;
    }

    /// <summary>
    /// Устанавливает модуль источника тока и напряжения.
    /// </summary>
    /// <param name="deviceModel">Модель устройства МИНТ.</param>
    static public void SetMint(ModuleVoltageCurrentSource.Model deviceModel)
    {
      ModuleVoltageCurrentSource = deviceModel;
    }

    /// <summary>
    /// Устанавливает менеджер шасси.
    /// </summary>
    /// <param name="deviceModel">Модель устройства менеджера шасси.</param>
    static public void SetManagerShassy(ManagerShassy.Model deviceModel)
    {
      ManagerShassy = deviceModel;
    }

    /// <summary>
    /// Устанавливает УКШ.
    /// </summary>
    /// <param name="deviceModel">Модель устройства УКШ.</param>
    static public void SetDeviceBusCommunication(DeviceBusCommutation.Model deviceModel)
    {
      DeviceBusCommunication = deviceModel;
    }

    /// <summary>
    /// Добавляет блок модуля коммутации реле.
    /// </summary>
    /// <param name="deviceModel">Модель устройств МКР.</param>
    static public void AddMkrModels(ModuleRelayControl.Model deviceModel)
    {
      if (ModuleRelayControlsList == null)
      {
        ModuleRelayControlsList = new List<ModuleRelayControl.Model>();
      }

      ModuleRelayControlsList.Add(deviceModel);
    }

    /// <summary>
    /// Устанавливает блоки мкр.
    /// </summary>
    /// <param name="deviceModels">Модели устройств МКР.</param>
    static public void SetMkrModels(List<ModuleRelayControl.Model> deviceModels)
    {
      ModuleRelayControlsList = deviceModels;
    }
  }
}
