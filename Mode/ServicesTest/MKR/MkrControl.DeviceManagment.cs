using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mode.ServicesTest.MKR
{
  public partial class MkrControl
  {
    /// <summary>
    /// Подключает или отключает устройство (вместо ToggleModuleButton).
    /// </summary>
    public async Task<bool> AttemptDeviceConnectionAsync()
    {
      if (!isMkrInitialized)
      {
        await ShowMessageAsync("Сначала выберите устройство!");
        return false;
      }

      isConnected = !isConnected;

      if (isConnected)
      {
        // Условно "ЗАПУСТИЛИ"
        BtnConnect.Content = "ОСТАНОВИТЬ";
        await ShowMessageAsync($"Подключение измерителя: {currentDeviceName}");
      }
      else
      {
        // "ОСТАНОВИЛИ"
        BtnConnect.Content = "ЗАПУСТИТЬ";
        await ShowMessageAsync($"Отключение измерителя: {currentDeviceName}");
      }

      // Возвращаем текущее состояние
      return isConnected;
    }

    /// <summary>
    /// Сбрасывает устройство, приводя всё к начальному состоянию.
    /// </summary>
    public async Task ResetMkrDevice()
    {
      // Если было подключено — отключаем
      if (isConnected)
      {
        await AttemptDeviceConnectionAsync();
      }

      offA.IsChecked = true;
      offB.IsChecked = true;

      // Сброс точек
      foreach (var point in points)
      {
        point.A = false;
        point.B = false;
      }

      // Логирование
      if (!string.IsNullOrEmpty(currentDeviceName))
        await ShowMessageAsync($"Сброс {currentDeviceName}");
      else
        await ShowMessageAsync("Сброс устройства (не выбрано имя)");
    }

    /// <summary>
    /// Включает/выключает доступность некоторых кнопок и полей.
    /// </summary>
    /// <param name="enable">True - включить, False - выключить.</param>
    /// <param name="skipLog">Если true, не выводить лог.</param>
    public async Task UpdateMkrUI(bool enable, bool skipLog = false)
    {
      isMkrInitialized = enable;

      BtnMkrReset.IsEnabled = enable;
      BtnConnect.IsEnabled = enable;

      ToggleRadioButtonState("BusA", enable);
      ToggleRadioButtonState("BusB", enable);

      SearchBox.IsEnabled = enable;
      PointsListBox.IsEnabled = enable;

      // Если подключено, ComboBox отключаем
      SerialNumComboBox.IsEnabled = !isConnected;

      if (!skipLog)
      {
        if (enable)
        {
          await ShowMessageAsync($"Инициализация {currentDeviceName}");
        }
        else
        {
          if (!string.IsNullOrEmpty(currentDeviceName))
            await ShowMessageAsync($"Отключение {currentDeviceName}");
        }
      }
    }
  }
}
