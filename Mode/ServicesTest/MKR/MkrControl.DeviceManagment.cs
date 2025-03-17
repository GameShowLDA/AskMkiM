namespace Mode.ServicesTest.MKR
{
  /// <summary>
  /// Управляет состоянием устройства МКР, включая его инициализацию, сброс параметров и обновление пользовательского интерфейса.
  /// </summary>
  public partial class MkrControl
  {
    /// <summary>
    /// Подключает или отключает устройство (вместо ToggleModuleButton).
    /// Возвращает текущее состояние подключения: <c>true</c> – устройство подключено, <c>false</c> – отключено.
    /// </summary>
    /// <returns>Состояние подключения устройства.</returns>
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
        BtnConnect.Content = "ОСТАНОВИТЬ";
        await ShowMessageAsync($"Подключение измерителя: {currentDeviceName}");
      }
      else
      {
        BtnConnect.Content = "ЗАПУСТИТЬ";
        await ShowMessageAsync($"Отключение измерителя: {currentDeviceName}");
      }

      return isConnected;
    }

    /// <summary>
    /// Сбрасывает устройство, приводя его к начальному состоянию.
    /// Осуществляется отключение подключения, сброс состояния радиокнопок и точек.
    /// </summary>
    public async Task ResetMkrDevice()
    {
      if (isConnected)
      {
        await AttemptDeviceConnectionAsync();
      }

      RbOffA.IsChecked = true;
      RbOffB.IsChecked = true;

      // Сброс состояния точек с использованием DeferRefresh для минимизации обновлений UI.
      using (pointsView.DeferRefresh())
      {
        foreach (var point in points)
        {
          point.A = false;
          point.B = false;
        }
      }

      if (!string.IsNullOrEmpty(currentDeviceName))
        await ShowMessageAsync($"Сброс {currentDeviceName}");
      else
        await ShowMessageAsync("Сброс устройства (не выбрано имя)");
    }

    /// <summary>
    /// Включает или отключает доступность элементов управления в зависимости от состояния устройства.
    /// Обновляет состояние кнопок, поля поиска, списка точек и ComboBox.
    /// </summary>
    /// <param name="enable">
    /// Если <c>true</c>, элементы управления становятся доступными (устройство инициализировано);
    /// если <c>false</c>, они отключаются.
    /// </param>
    /// <param name="skipLog">
    /// Если <c>true</c>, логирование изменений состояния не производится.
    /// </param>
    public async Task UpdateMkrUI(bool enable, bool skipLog = false)
    {
      isMkrInitialized = enable;

      BtnMkrReset.IsEnabled = enable;
      BtnConnect.IsEnabled = enable;

      ToggleRadioButtonState(enable);

      SearchBox.IsEnabled = enable;
      PointsListBox.IsEnabled = enable;

      // Если устройство подключено, отключаем выбор устройства в ComboBox.
      SerialNumComboBox.IsEnabled = !isConnected;

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
  }
}