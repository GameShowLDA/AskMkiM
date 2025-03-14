using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Mode.ServicesTest.MINT
{
  public partial class MintControl
  {
    /// <summary>
    /// ComboBox: если "<пусто>", сбрасываем, иначе инициализируем
    /// </summary>
    private async void CmbMintDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var selectedItem = CmbMintDevice.SelectedItem as string;
      if (string.IsNullOrEmpty(selectedItem) || selectedItem == "<пусто>")
      {
        if (isDeviceInitialized)
        {
          // Сброс параметров
          ResetMintParameters();
          await ShowMessageAsync("Устройство отключено");
        }
        isDeviceInitialized = false;
        currentDeviceName = string.Empty;

        // Переводим UI в самое начало
        InitializeMintUI();
        // Можно вызвать UpdateMintUI(false, skipLog:true) чтобы всё отключить.
        await UpdateMintUI(false, skipLog: true);
      }
      else
      {
        // Если уже инициализировано другое устройство, сброс
        if (isDeviceInitialized && currentDeviceName != selectedItem)
        {
          ResetMintParameters();
          await ShowMessageAsync($"Сброс {currentDeviceName}");
        }
        isDeviceInitialized = true;
        currentDeviceName = selectedItem;

        // Включаем всё и логируем
        await UpdateMintUI(true, skipLog: false);
      }
    }

    /// <summary>
    /// Кнопка "Сброс устройства"
    /// </summary>
    private async void BtnMintReset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      // Сброс параметров
      ResetMintParameters();
      if (!string.IsNullOrEmpty(currentDeviceName))
        await ShowMessageAsync($"Сброс {currentDeviceName}");
      else
        await ShowMessageAsync("Сброс устройства (не выбрано имя)");
    }

    // ----------- Слайдер/ТекстБокс для ПИН -----------
    private async void SliderMintPinVoltage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (isDeviceInitialized)
      {
        double val = e.NewValue;
        TextMintPinVoltage.Text = val.ToString("F2", CultureInfo.InvariantCulture);
        await ShowMessageAsync($"Установлено напряжение ПИН: {val:F2}");
      }
    }

    private void TextMintPinVoltage_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (isDeviceInitialized)
      {
        if (double.TryParse(TextMintPinVoltage.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
        {
          if (val < 0) val = 0;
          if (val > 100) val = 100;
          SliderMintPinVoltage.Value = val;
        }
      }
    }

    private void TextMintPinVoltage_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      // Разрешаем только цифры, точку, запятую
      if (!Regex.IsMatch(e.Text, @"^[0-9\.,]+$"))
        e.Handled = true;
    }

    // ----------- Слайдер/ТекстБокс для ПИТ -----------
    private async void SliderMintPitAmperage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (isDeviceInitialized)
      {
        double val = e.NewValue;
        TextMintPitAmperage.Text = val.ToString("F0", CultureInfo.InvariantCulture);
        await ShowMessageAsync($"Установлена сила тока ПИТ: {val}");
      }
    }

    private void TextMintPitAmperage_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (isDeviceInitialized)
      {
        if (double.TryParse(TextMintPitAmperage.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
        {
          if (val < 0) val = 0;
          if (val > 100) val = 100;
          SliderMintPitAmperage.Value = val;
        }
      }
    }

    private void TextMintPitAmperage_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (!Regex.IsMatch(e.Text, @"^[0-9\.,]+$"))
        e.Handled = true;
    }

    // ----------- Радио-кнопки k/k+ и заземление -----------
    private void RbMintK_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      BtnMintGround.IsEnabled = true;
    }

    private void RbMintKplus_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      BtnMintGround.IsEnabled = true;
    }

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

      // Вызываем UpdateMintUI: если заземлено, нужно отключить кнопки?
      await UpdateMintUI(isDeviceInitialized && !IsAnyModuleConnected(), skipLog: true);
    }

    // ----------- Источник питания -----------
    private async void RbMintPower12v_Checked(object sender, RoutedEventArgs e)
    {
      await ShowMessageAsync("Выбран источник питания: 12В");
      await UpdateMintUI(isDeviceInitialized, skipLog: true);
    }


    private async void RbMintPower48v_Checked(object sender, RoutedEventArgs e)
    {
      await ShowMessageAsync("Выбран источник питания: 48В");
      await UpdateMintUI(isDeviceInitialized, skipLog: true);
    }


    // ----------- Кнопки ПИН / ПИТ -----------
    private async void BtnMintPin_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      isMintPinConnected = !isMintPinConnected;
      BtnMintPin.Content = isMintPinConnected ? "Отключение ПИН" : "Подключение ПИН";
      await ShowMessageAsync(isMintPinConnected ? "Подключение ПИН" : "Отключение ПИН");

      // После подключения ПИН — возможно частично блокируем UI
      await UpdateMintUI(isDeviceInitialized, skipLog: true);
    }


    private async void BtnMintPit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      isMintPitConnected = !isMintPitConnected;
      BtnMintPit.Content = isMintPitConnected ? "Отключение ПИТ" : "Подключение ПИТ";
      await ShowMessageAsync(isMintPitConnected ? "Подключение ПИТ" : "Отключение ПИТ");

      await UpdateMintUI(isDeviceInitialized, skipLog: true);
    }

    /// <summary>
    /// Проверяем, подключен ли хотя бы один модуль (ПИТ или ПИН).
    /// Если да — часть UI должна блокироваться.
    /// </summary>
    private bool IsAnyModuleConnected()
    {
      return isMintPinConnected || isMintPitConnected;
    }

    /// <summary>
    /// Вывод лога в protocolTextBox
    /// </summary>
    private Task ShowMessageAsync(string text)
    {
      protocolTextBox?.ShowMessageAsync($"{text}\n");
      protocolTextBox?.ScrollToEnd();
      return Task.CompletedTask;
    }
  }
}
