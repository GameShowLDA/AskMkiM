namespace Mode.ServicesTest.MINT
{
  /// <summary>
  /// Управляет состоянием устройства МИНТ, включая его инициализацию, сброс параметров и обновление пользовательского интерфейса.
  /// </summary>
  public partial class MintControl
  {
    /// <summary>
    /// Сбрасывает состояние устройства к начальному (неинициализированному).
    /// </summary>
    private void InitializeMintUI()
    {
      isDeviceInitialized = false;
      currentDeviceName = string.Empty;

      // Настройка ComboBox для выбора устройства.
      CmbMintDevice.IsEnabled = true;
      CmbMintDevice.SelectedItem = "<пусто>";

      // Настройка кнопки "Сброс устройства".
      BtnMintReset.Content = "Сброс устройства";
      BtnMintReset.IsEnabled = false;

      // Сброс значений и блокировка элементов для регулировки напряжения ПИН.
      SliderMintPinVoltage.Value = 0;
      SliderMintPinVoltage.IsEnabled = false;
      TextMintPinVoltage.Text = "0";
      TextMintPinVoltage.IsEnabled = false;

      // Сброс значений и блокировка элементов для регулировки силы тока ПИТ.
      SliderMintPitAmperage.Value = 0;
      SliderMintPitAmperage.IsEnabled = false;
      TextMintPitAmperage.Text = "0";
      TextMintPitAmperage.IsEnabled = false;

      // Сброс состояния радиокнопок для выбора шины и кнопки заземления.
      RbMintK.IsChecked = false;
      RbMintK.IsEnabled = false;
      RbMintKplus.IsChecked = false;
      RbMintKplus.IsEnabled = false;
      BtnMintGround.Content = "Заземлить шину";
      BtnMintGround.IsEnabled = false;
      BtnMintGround.Tag = false;
      btnMintGroundStatus = false;

      // Сброс состояния источника питания.
      RbMintPower12v.IsChecked = false;
      RbMintPower48v.IsChecked = false;
      RbMintPower12v.IsEnabled = false;
      RbMintPower48v.IsEnabled = false;

      // Сброс состояния кнопок подключения модулей ПИН и ПИТ.
      BtnMintPin.Content = "Подключение ПИН";
      BtnMintPin.IsEnabled = false;
      isMintPinConnected = false;

      BtnMintPit.Content = "Подключение ПИТ";
      BtnMintPit.IsEnabled = false;
      isMintPitConnected = false;
    }

    /// <summary>
    /// Сбрасывает параметры устройства, не отключая его полностью.
    /// Восстанавливает начальные значения элементов управления, разрешает выбор устройства и логирует процесс сброса.
    /// </summary>
    private async void ResetMintParameters()
    {
      // Сброс значений и активация элементов для регулировки напряжения ПИН.
      SliderMintPinVoltage.Value = 0;
      SliderMintPinVoltage.IsEnabled = true;
      TextMintPinVoltage.Text = "0";
      TextMintPinVoltage.IsEnabled = true;

      // Сброс значений и активация элементов для регулировки силы тока ПИТ.
      SliderMintPitAmperage.Value = 0;
      SliderMintPitAmperage.IsEnabled = true;
      TextMintPitAmperage.Text = "0";
      TextMintPitAmperage.IsEnabled = true;

      // Сброс и активация радиокнопок для выбора шины.
      RbMintK.IsChecked = false;
      RbMintKplus.IsChecked = false;
      RbMintK.IsEnabled = true;
      RbMintKplus.IsEnabled = true;

      // Сброс состояния кнопки заземления.
      BtnMintGround.Content = "Заземлить шину";
      BtnMintGround.IsEnabled = false;
      BtnMintGround.Tag = false;
      btnMintGroundStatus = false;

      // Сброс и активация элементов источника питания.
      RbMintPower12v.IsChecked = false;
      RbMintPower48v.IsChecked = false;
      RbMintPower12v.IsEnabled = true;
      RbMintPower48v.IsEnabled = true;

      // Сброс состояния кнопок подключения модулей ПИН и ПИТ.
      BtnMintPin.Content = "Подключение ПИН";
      BtnMintPin.IsEnabled = false;
      isMintPinConnected = false;

      BtnMintPit.Content = "Подключение ПИТ";
      BtnMintPit.IsEnabled = false;
      isMintPitConnected = false;

      // Разрешение выбора устройства.
      CmbMintDevice.IsEnabled = true;

      if (!string.IsNullOrEmpty(currentDeviceName))
        await ShowMessageAsync($"Сброс {currentDeviceName}");
      else
        await ShowMessageAsync("Сброс устройства (не выбрано имя)");
    }

    /// <summary>
    /// Обновляет состояние элементов управления на основе текущего состояния устройства и подключенных модулей.
    /// Определяет доступность кнопок, слайдеров, текстовых полей, радиокнопок, источника питания и ComboBox.
    /// Логирует изменения, если параметр <paramref name="skipLog"/> равен false.
    /// </summary>
    /// <param name="enable">Если true, устройство инициализировано и доступно для управления; иначе false.</param>
    /// <param name="skipLog">Если true, логирование не производится.</param>
    public async Task UpdateMintUI(bool enable, bool skipLog)
    {
      bool isPowerChosen = (RbMintPower12v.IsChecked ?? false) || (RbMintPower48v.IsChecked ?? false);
      bool lockNeeded = isMintPinConnected || isMintPitConnected;

      // Основной флаг инициализации устройства.
      isDeviceInitialized = enable;

      // Обновление состояния кнопки "Сброс устройства".
      BtnMintReset.IsEnabled = enable && !lockNeeded;

      // Обновление состояния слайдеров и текстовых полей.
      SliderMintPinVoltage.IsEnabled = enable && !lockNeeded;
      TextMintPinVoltage.IsEnabled = enable && !lockNeeded;
      SliderMintPitAmperage.IsEnabled = enable && !lockNeeded;
      TextMintPitAmperage.IsEnabled = enable && !lockNeeded;

      // Обновление состояния радиокнопок для выбора шины.
      RbMintK.IsEnabled = enable && !lockNeeded && !btnMintGroundStatus;
      RbMintKplus.IsEnabled = enable && !lockNeeded && !btnMintGroundStatus;
      bool kChosen = (RbMintK.IsChecked ?? false) || (RbMintKplus.IsChecked ?? false);
      BtnMintGround.IsEnabled = enable && !lockNeeded && kChosen; // Убрано условие !btnMintGroundStatus


      // Обновление состояния элементов источника питания.
      RbMintPower12v.IsEnabled = enable && !lockNeeded;
      RbMintPower48v.IsEnabled = enable && !lockNeeded;

      // Обновление состояния кнопок подключения модулей ПИН и ПИТ.
      bool canUsePinPit = enable && (isPowerChosen || isMintPinConnected || isMintPitConnected);
      BtnMintPin.IsEnabled = canUsePinPit;
      BtnMintPit.IsEnabled = canUsePinPit;

      // Обновление состояния ComboBox выбора устройства.
      CmbMintDevice.IsEnabled = !lockNeeded || !enable;

      if (!skipLog)
      {
        if (enable)
          await ShowMessageAsync($"Инициализация {currentDeviceName}");
        else if (!string.IsNullOrEmpty(currentDeviceName))
          await ShowMessageAsync($"Отключение {currentDeviceName}");
      }
    }
  }
}