using Mode.Models;

namespace Mode.ServicesTest.UKSH
{
  /// <summary>
  /// Управляет состоянием устройства УКШ, включая его инициализацию, сброс параметров и обновление пользовательского интерфейса.
  /// </summary>
  public partial class UkshControl
  {
    /// <summary>
    /// Фильтрует реле по тексту поиска.
    /// Если <paramref name="searchText"/> пуст или содержит только пробелы, фильтр убирается.
    /// Иначе осуществляется фильтрация по строковому представлению номера реле.
    /// </summary>
    /// <param name="searchText">Текст для фильтрации реле.</param>
    private void FilterRelays(string searchText)
    {
      if (string.IsNullOrWhiteSpace(searchText))
      {
        relaysView.Filter = null;
      }
      else
      {
        string lowerSearch = searchText.ToLowerInvariant();
        relaysView.Filter = r =>
        {
          if (r is RelayModel relay)
          {
            return relay.RelayNumString.ToLowerInvariant().Contains(lowerSearch);
          }
          return false;
        };
      }
      relaysView.Refresh();
    }

    /// <summary>
    /// Обновляет состояние элементов управления в зависимости от инициализации устройства.
    /// Обновляются доступность кнопок, поля поиска, список реле и ComboBox.
    /// </summary>
    /// <param name="enable">Если true, устройство инициализировано.</param>
    /// <param name="skipLog">Если true, лог не выводится.</param>
    private async Task UpdateUkshUI(bool enable, bool skipLog)
    {
      isUkshInitialized = enable;

      BtnUkshReset.IsEnabled = enable;
      BtnUkshStart.IsEnabled = enable;
      TbSearchRelays.IsEnabled = enable;
      IcRelays.IsEnabled = enable;

      CmbUkshInit.IsEnabled = !isShinaConnected;

      if (!skipLog)
      {
        if (enable)
        {
          await ShowMessageAsync($"Инициализация {currentDeviceName}");
        }
        else if (!string.IsNullOrEmpty(currentDeviceName))
        {
          await ShowMessageAsync($"Отключение {currentDeviceName}");
        }
      }
    }

    /// <summary>
    /// Сбрасывает состояние устройства: отключает шину, сбрасывает состояние всех реле и очищает поиск.
    /// </summary>
    /// <param name="skipLog">Если true, логирование не производится.</param>
    private async Task ResetUkshDevice(bool skipLog = false)
    {
      if (isShinaConnected)
      {
        isShinaConnected = false;
        TbSearchRelays.IsEnabled = true;
        IcRelays.IsEnabled = true;
        await ShowMessageAsync("Отключение шины");
      }

      using (relaysView.DeferRefresh())
      {
        foreach (var r in allRelays)
          r.IsOn = false;
      }

      TbSearchRelays.Text = "";
      FilterRelays("");

      if (!string.IsNullOrEmpty(currentDeviceName))
        await ShowMessageAsync($"Сброс {currentDeviceName}");
      else
        await ShowMessageAsync("Сброс устройства (не выбрано имя)");
    }
  }
}