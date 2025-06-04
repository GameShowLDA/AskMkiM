using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Mode.ServicesTest.MINT
{
  /// <summary>
  /// Обработчики событий для управления устройством MINT.
  /// </summary>
  public partial class MintControl
  {
    /// <summary>
    /// Обрабатывает изменение выбранного элемента в ComboBox для выбора устройства.
    /// Если выбран элемент "<пусто>" или пустая строка, выполняется сброс параметров устройства.
    /// </summary>
    /// <param name="sender">ComboBox, содержащий список устройств.</param>
    /// <param name="e">Аргументы события изменения выбора.</param>
    private async void CmbMintDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var selectedItem = CmbMintDevice.SelectedItem as string;
      if (string.IsNullOrEmpty(selectedItem) || selectedItem == "<пусто>")
      {
        if (isDeviceInitialized)
        {
          ResetMintParameters();
          await ShowMessageAsync("Устройство отключено");
        }
        isDeviceInitialized = false;
        currentDeviceName = string.Empty;

        // Переводим UI в исходное состояние.
        InitializeMintUI();
        await UpdateMintUI(false, skipLog: true);
      }
      else
      {
        if (isDeviceInitialized && currentDeviceName != selectedItem)
        {
          ResetMintParameters();
          await ShowMessageAsync($"Сброс {currentDeviceName}");
        }
        isDeviceInitialized = true;
        currentDeviceName = selectedItem;
        await UpdateMintUI(true, skipLog: false);
      }
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки "Сброс устройства".
    /// Выполняет сброс параметров и выводит соответствующее сообщение.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void BtnMintReset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      ResetMintParameters();
      if (!string.IsNullOrEmpty(currentDeviceName))
        await ShowMessageAsync($"Сброс {currentDeviceName}");
      else
        await ShowMessageAsync("Сброс устройства (не выбрано имя)");
    }

    #region Регулятор напряжения ПИН

    /// <summary>
    /// Обрабатывает изменение значения слайдера для напряжения ПИН.
    /// Обновляет текстовое поле и выводит сообщение с новым значением.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Slider.</param>
    /// <param name="e">Аргументы события изменения значения.</param>
    private async void SliderMintPinVoltage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (isDeviceInitialized)
      {
        double val = e.NewValue;
        TextMintPinVoltage.Text = val.ToString("F2", CultureInfo.InvariantCulture);
        await ShowMessageAsync($"Установлено напряжение ПИН: {val:F2}");
      }
    }

    /// <summary>
    /// Обновляет значение слайдера напряжения ПИН на основе текста из TextMintPinVoltage.
    /// Ограничивает значение в диапазоне 0–100.
    /// </summary>
    private void UpdateSliderFromTextBox()
    {
      if (isDeviceInitialized)
      {
        if (double.TryParse(TextMintPinVoltage.Text.Replace(',', '.'),
                            NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
        {
          if (val < 0) val = 0;
          if (val > 100) val = 100;
          SliderMintPinVoltage.Value = val;
        }
      }
    }

    /// <summary>
    /// Обрабатывает событие потери фокуса TextMintPinVoltage.
    /// Вызывает обновление значения слайдера.
    /// </summary>
    /// <param name="sender">TextBox для ввода напряжения ПИН.</param>
    /// <param name="e">Аргументы события.</param>
    private void TextMintPinVoltage_LostFocus(object sender, RoutedEventArgs e)
    {
      UpdateSliderFromTextBox();
    }

    /// <summary>
    /// Обрабатывает нажатие клавиши в TextMintPinVoltage.
    /// При нажатии клавиши Enter обновляет значение слайдера.
    /// </summary>
    /// <param name="sender">TextBox для ввода напряжения ПИН.</param>
    /// <param name="e">Аргументы события нажатия клавиши.</param>
    private void TextMintPinVoltage_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        UpdateSliderFromTextBox();
      }
    }

    /// <summary>
    /// Обрабатывает предварительный ввод в TextMintPinVoltage.
    /// Разрешает ввод только цифр, точки и запятой.
    /// </summary>
    /// <param name="sender">TextBox для ввода напряжения ПИН.</param>
    /// <param name="e">Аргументы события ввода текста.</param>
    private void TextMintPinVoltage_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (!Regex.IsMatch(e.Text, @"^[0-9\.,]+$"))
        e.Handled = true;
    }

    #endregion

    #region Регулятор силы тока ПИТ

    /// <summary>
    /// Обрабатывает изменение значения слайдера для силы тока ПИТ.
    /// Обновляет текстовое поле и выводит сообщение с новым значением.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Slider.</param>
    /// <param name="e">Аргументы события изменения значения.</param>
    private async void SliderMintPitAmperage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (isDeviceInitialized)
      {
        double val = e.NewValue;
        TextMintPitAmperage.Text = val.ToString("F0", CultureInfo.InvariantCulture);
        await ShowMessageAsync($"Установлена сила тока ПИТ: {val}");
      }
    }

    /// <summary>
    /// Обновляет значение слайдера силы тока ПИТ на основе текста из TextMintPitAmperage.
    /// Ограничивает значение в диапазоне 0–100.
    /// </summary>
    private void UpdateSliderFromTextBox_Pit()
    {
      if (isDeviceInitialized)
      {
        if (double.TryParse(TextMintPitAmperage.Text.Replace(',', '.'),
                            NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
        {
          if (val < 0) val = 0;
          if (val > 100) val = 100;
          SliderMintPitAmperage.Value = val;
        }
      }
    }

    /// <summary>
    /// Обрабатывает событие потери фокуса TextMintPitAmperage.
    /// Вызывает обновление значения слайдера.
    /// </summary>
    /// <param name="sender">TextBox для ввода силы тока ПИТ.</param>
    /// <param name="e">Аргументы события.</param>
    private void TextMintPitAmperage_LostFocus(object sender, RoutedEventArgs e)
    {
      UpdateSliderFromTextBox_Pit();
    }

    /// <summary>
    /// Обрабатывает нажатие клавиши в TextMintPitAmperage.
    /// При нажатии клавиши Enter обновляет значение слайдера.
    /// </summary>
    /// <param name="sender">TextBox для ввода силы тока ПИТ.</param>
    /// <param name="e">Аргументы события нажатия клавиши.</param>
    private void TextMintPitAmperage_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        UpdateSliderFromTextBox_Pit();
      }
    }

    /// <summary>
    /// Обрабатывает предварительный ввод в TextMintPitAmperage.
    /// Разрешает ввод только цифр, точки и запятой.
    /// </summary>
    /// <param name="sender">TextBox для ввода силы тока ПИТ.</param>
    /// <param name="e">Аргументы события ввода текста.</param>
    private void TextMintPitAmperage_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (!Regex.IsMatch(e.Text, @"^[0-9]+$"))
        e.Handled = true;
    }

    #endregion

    #region Радио-кнопки и заземление

    /// <summary>
    /// Обрабатывает нажатие на радио-кнопку выбора шины "k".
    /// Активирует кнопку заземления.
    /// </summary>
    /// <param name="sender">Источник события, ожидается RadioButton.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void RbMintK_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      BtnMintGround.IsEnabled = true;
    }

    /// <summary>
    /// Обрабатывает нажатие на радио-кнопку выбора шины "k+".
    /// Активирует кнопку заземления.
    /// </summary>
    /// <param name="sender">Источник события, ожидается RadioButton.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void RbMintKplus_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      BtnMintGround.IsEnabled = true;
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки заземления шины.
    /// Переключает состояние заземления, обновляет текст кнопки и выводит сообщение.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void BtnMintGround_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      btnMintGroundStatus = !btnMintGroundStatus;
      if (btnMintGroundStatus)
      {
        BtnMintGround.Content = "Отключить заземление шины";
        await ShowMessageAsync($"Шина {(RbMintK.IsChecked == true ? "k" : "k+")} заземлена");
      }
      else
      {
        BtnMintGround.Content = "Заземлить шину";
        await ShowMessageAsync($"Отключение заземления шины {(RbMintK.IsChecked == true ? "k" : "k+")}");
      }
      await UpdateMintUI(isDeviceInitialized && !IsAnyModuleConnected(), skipLog: true);
    }

    #endregion

    #region Источник питания

    /// <summary>
    /// Обрабатывает выбор источника питания 12В.
    /// Выводит сообщение и обновляет состояние UI.
    /// </summary>
    /// <param name="sender">Источник события, ожидается RadioButton.</param>
    /// <param name="e">Аргументы события.</param>
    private async void RbMintPower12v_Checked(object sender, RoutedEventArgs e)
    {
      await ShowMessageAsync("Выбран источник питания: 12В");
      await UpdateMintUI(isDeviceInitialized, skipLog: true);
    }

    /// <summary>
    /// Обрабатывает выбор источника питания 48В.
    /// Выводит сообщение и обновляет состояние UI.
    /// </summary>
    /// <param name="sender">Источник события, ожидается RadioButton.</param>
    /// <param name="e">Аргументы события.</param>
    private async void RbMintPower48v_Checked(object sender, RoutedEventArgs e)
    {
      await ShowMessageAsync("Выбран источник питания: 48В");
      await UpdateMintUI(isDeviceInitialized, skipLog: true);
    }

    #endregion

    #region Кнопки подключения модулей ПИН/ПИТ

    /// <summary>
    /// Обрабатывает нажатие кнопки подключения/отключения модуля ПИН.
    /// Переключает состояние подключения и обновляет UI.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void BtnMintPin_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      isMintPinConnected = !isMintPinConnected;
      BtnMintPin.Content = isMintPinConnected ? "Отключение ПИН" : "Подключение ПИН";
      await ShowMessageAsync(isMintPinConnected ? "Подключение ПИН" : "Отключение ПИН");
      await UpdateMintUI(isDeviceInitialized, skipLog: true);
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки подключения/отключения модуля ПИТ.
    /// Переключает состояние подключения и обновляет UI.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void BtnMintPit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      isMintPitConnected = !isMintPitConnected;
      BtnMintPit.Content = isMintPitConnected ? "Отключение ПИТ" : "Подключение ПИТ";
      await ShowMessageAsync(isMintPitConnected ? "Подключение ПИТ" : "Отключение ПИТ");
      await UpdateMintUI(isDeviceInitialized, skipLog: true);
    }

    #endregion

    /// <summary>
    /// Проверяет, подключен ли хотя бы один модуль (ПИН или ПИТ).
    /// </summary>
    /// <returns>
    /// <c>true</c>, если хотя бы один модуль подключен; иначе <c>false</c>.
    /// </returns>
    private bool IsAnyModuleConnected()
    {
      return isMintPinConnected || isMintPitConnected;
    }

    /// <summary>
    /// Асинхронно выводит сообщение в элемент протокола (protocolTextBox).
    /// </summary>
    /// <param name="text">Текст сообщения.</param>
    /// <returns>Задача, представляющая завершение асинхронной операции.</returns>
    private Task ShowMessageAsync(string text)
    {
      ProtocolSelfCheckControl?.ShowMessageAsync(new Utilities.Models.ShowMessageModel(text));
      return Task.CompletedTask;
    }
  }
}