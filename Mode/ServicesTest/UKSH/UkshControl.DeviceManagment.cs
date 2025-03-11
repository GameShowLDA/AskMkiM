using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mode.Models;
using Mode.ServicesTest.Helpers;

namespace Mode.ServicesTest.UKSH
{
  public partial class UkshControl
  {
    /// <summary>
    /// Фильтрует реле по тексту поиска.
    /// </summary>
    private void FilterRelays(string searchText)
    {
      Relays.Clear();
      if (string.IsNullOrWhiteSpace(searchText))
      {
        foreach (var r in allRelays)
          Relays.Add(r);
      }
      else
      {
        foreach (var r in allRelays)
        {
          if (r.RelayNum.ToString().IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
          {
            Relays.Add(r);
          }
        }
      }
    }

    /// <summary>
    /// Обновляет состояние элементов управления в зависимости от инициализации устройства.
    /// </summary>
    /// <param name="enable">True, если устройство инициализировано.</param>
    /// <param name="skipLog">Если true, лог не выводим.</param>
    private async Task UpdateUkshUI(bool enable, bool skipLog)
    {
      isUkshInitialized = enable;

      BtnUkshReset.IsEnabled = enable;
      // Кнопка "ЗАПУСТИТЬ" тоже может зависеть от enable
      BtnUkshStart.IsEnabled = enable;
      TbSearchRelays.IsEnabled = enable;
      IcRelays.IsEnabled = enable;

      // Если шина подключена, запрещаем менять устройство
      CmbUkshInit.IsEnabled = !isShinaConnected;

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

    /// <summary>
    /// Сбрасывает состояние устройства (отключает шину, сбрасывает реле, очищает поиск).
    /// </summary>
    private async Task ResetUkshDevice(bool skipLog = false)
    {
      // Если шина подключена, отключаем
      if (isShinaConnected)
      {
        // Можно имитировать логику, как если бы мы вызывали BtnUkshStart
        // или какой-то отдельный метод "DisconnectShinaAsync()"
        isShinaConnected = false;
        TbSearchRelays.IsEnabled = true;
        IcRelays.IsEnabled = true;
        await ShowMessageAsync("Отключение шины");
        // CmbUkshInit.IsEnabled = true;
      }

      // Сбрасываем все реле
      foreach (var r in allRelays)
        r.IsOn = false;

      // Сбрасываем поиск
      TbSearchRelays.Text = "";
      FilterRelays("");

      if (!string.IsNullOrEmpty(currentDeviceName))
        await ShowMessageAsync($"Сброс {currentDeviceName}");
      else
        await ShowMessageAsync("Сброс устройства (не выбрано имя)");
    }
  }
}
