using Core.Abstract;
using Core.Enum;
using Core.Model;
using static Core.ConfigCollector.ConfigCollector;
using static Utilities.LoggerUtility;

namespace AppConfig.Data.Device
{
  /// <summary>
  /// Класс для работы с настройками устройств.
  /// </summary>
  static internal class DeviceSettingsManager
  {
    /// <summary>
    /// Асинхронно читает и обрабатывает конфигурацию устройств.
    /// </summary>
    static internal async Task ReadDeviceModeAsync()
    {
      var deviceFileManager = new DeviceFileManager(FileLocations.ConfigFilePath);

      if (!deviceFileManager.CreateFileIfNotExists())
      {
        return;
      }

      ClearMkrModel();

      var devicesObject = await deviceFileManager.ReadFileAsync();
      if (devicesObject == null || devicesObject.Count == 0)
      {
        LogWarning($"Конфигурация пуста или файл не содержит корректных данных. {FileLocations.ConfigFilePath}");
        return;
      }

      ProcessDevices(devicesObject);
    }

    /// <summary>
    /// Обрабатывает список устройств.
    /// </summary>
    /// <param name="devices">Список устройств.</param>
    private static void ProcessDevices(List<object> devices)
    {
      foreach (var device in devices)
      {
        if (device == null)
        {
          LogWarning("Обнаружено null-устройство в конфигурации.");
          continue;
        }
        var type = DeviceModel.TryGetDeviceTypeFromObject(device);

        if (type == null)
        {
          LogWarning("Не удалось определить тип устройства.");
          continue;
        }

        try
        {
          ProcessDeviceByType(device, type.Value);
        }
        catch (InvalidOperationException ex)
        {
          LogError($"Ошибка при установке устройства типа {type}: {ex.Message}");
          throw;
        }
        catch (ArgumentException ex)
        {
          LogError($"Некорректные аргументы при установке устройства типа {type}: {ex.Message}");
          throw;
        }
        catch (Exception ex)
        {
          LogError($"Неожиданная ошибка при установке устройства типа {type}: {ex.Message}");
          throw;
        }
      }
    }

    /// <summary>
    /// Обрабатывает устройство в зависимости от его типа.
    /// </summary>
    /// <param name="device">Устройство.</param>
    /// <param name="type">Тип устройства.</param>
    private static void ProcessDeviceByType(object device, DeviceEnum.Type type)
    {
      switch (type)
      {
        case DeviceEnum.Type.DeviceBusCommutation:
          var modelDBC = Core.DeviceBusCommutation.Model.CreateFromObject(device);
          if (modelDBC != null)
          {
            SetDeviceBusCommunication(modelDBC);
          }
          break;

        case DeviceEnum.Type.ModuleRelayControl:
          var modelMKR = Core.ModuleRelayControl.Model.CreateFromObject(device);
          if (modelMKR != null)
          {
            AddMkrModels(modelMKR);
          }
          break;

        case DeviceEnum.Type.ManagerShassy:
          var modelMS = Core.ManagerShassy.Model.CreateFromObject(device);
          if (modelMS != null)
          {
            SetManagerShassy(modelMS);
          }
          break;

        case DeviceEnum.Type.ModuleVoltageCurrentSource:
          var modelMINT = Core.ModuleVoltageCurrentSource.Model.CreateFromObject(device);
          if (modelMINT != null)
          {
            SetMint(modelMINT);
          }
          break;

        case DeviceEnum.Type.AccurateMeter:
          var modelAccurateMeter = MeterBase.CreateFromObject(device);
          SetAccurateMeter(device as MeterBase);
          break;

        case DeviceEnum.Type.FastMeter:
          var modelFastMeter = MeterBase.CreateFromObject(device);
          if (modelFastMeter != null)
          {
            SetFastMeter(device as MeterBase);
          }
          break;

        default:
          LogWarning($"Неизвестный тип устройства: {type}");
          break;
      }
    }
  }
}
