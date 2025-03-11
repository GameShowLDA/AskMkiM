using AppConfig.DataBase.Models;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Base;
using NewCore.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mode.Settings.DeviceConfig.Base
{
  public class DeviceSettingsProcessorBase
  {
    /// <summary>
    /// Метод создания и обработки модели устройства.
    /// </summary>
    /// <param name="selectedDevice">Интерфейс выбранного устройства.</param>
    /// <param name="connectionType">Тип подключения (DeviceWithIP или DeviceWithCom).</param>
    /// <param name="control">Элемент управления с настройками.</param>
    /// <param name="additionalDataProcessor">Внешний обработчик специфичных данных.</param>
    /// <returns>Заполненная модель устройства (реализующая интерфейс IDevice).</returns>
    public IDevice ProcessDevice(
        IDevice selectedDevice,
        DeviceSettingsControl control,
        IDataProcessor additionalDataProcessor = null)
    {
      // Создание конкретной модели устройства
      string connectString = BaseHandler<IDevice>.GetConnectionDetails(control, selectedDevice);

      var deviceModel = CreateDeviceModelByInterface(selectedDevice);

      deviceModel.Name = selectedDevice.Name;
      deviceModel.Description = selectedDevice.Description;
      deviceModel.ConnectionDetails = connectString;
      deviceModel.Number = BaseHandler<IDevice>.GetNumber(control);

      return deviceModel;
    }

    /// <summary>
    /// Создание конкретной модели по интерфейсу устройства.
    /// </summary>
    protected IDevice CreateDeviceModelByInterface(IDevice device)
    {
      return device switch
      {
        IBreakdownTester => new BreakdownTesterEntity(),
        IPowerSourceModule => new PowerSourceModuleEntity(),
        IPrecisionMeter => new PrecisionMeterEntity(),
        IRelaySwitchModule => new RelaySwitchModuleEntity(),
        ISwitchingDevice => new SwitchingDeviceEntity(),
        IChassisManager => new ChassisManagerEntity(),
        IFastMeter => new FastMeterEntity(),

        _ => throw new ArgumentException("Неизвестный тип устройства", nameof(device))
      };
    }

    /// <summary>
    /// Заполнение данных подключения устройства на основе типа подключения.
    /// </summary>
    /// <param name="model">Модель устройства.</param>
    /// <param name="connectionType">Тип подключения устройства (typeof(DeviceWithIP) или typeof(DeviceWithCOM)).</param>
    /// <param name="control">Элемент управления с настройками.</param>
    protected void FillConnectionSettings(IDevice model, Type connectionType, DeviceSettingsControl control)
    {
      if (connectionType == typeof(DeviceWithIP))
        FillIpSettings(model, control);
      else if (connectionType == typeof(DeviceWithCOM))
        FillComSettings(model, control);
      else
        throw new ArgumentException("Неизвестный тип подключения", nameof(connectionType));
    }

    /// <summary>
    /// Заполнение данных IP-подключения.
    /// </summary>
    protected virtual void FillIpSettings(IDevice model, DeviceSettingsControl control)
    {
      
    }

    /// <summary>
    /// Заполнение данных COM-подключения.
    /// </summary>
    protected virtual void FillComSettings(IDevice model, DeviceSettingsControl control)
    {
     
    }

    /// <summary>
    /// Валидация итоговой модели устройства.
    /// </summary>
    protected virtual void ValidateDeviceModel(IDevice model)
    {
      if (string.IsNullOrWhiteSpace(model.Name))
        throw new InvalidOperationException("Название устройства не может быть пустым!");

      // Другие проверки общие для всех устройств.
    }
  }
}
