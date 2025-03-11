using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Mode.Models;
using Mode.ServicesTest.Helpers;

namespace Mode.ServicesTest.UKSH
{
  public partial class UkshControl
  {
    /// <summary>
    /// Обработчик выбора устройства (CmbUkshInit).
    /// </summary>
    private async void CmbUkshInit_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      // Если шина подключена, менять нельзя
      if (isShinaConnected)
        return;

      var selected = CmbUkshInit.SelectedItem as string;
      if (string.IsNullOrEmpty(selected) || selected == "<пусто>")
      {
        if (isUkshInitialized)
        {
          await ResetUkshDevice();
          await ShowMessageAsync("Устройство отключено");
        }
        isUkshInitialized = false;
        currentDeviceName = string.Empty;
        await UpdateUkshUI(false, skipLog: true);
      }
      else
      {
        if (isUkshInitialized)
        {
          await ResetUkshDevice();
        }
        isUkshInitialized = true;
        currentDeviceName = selected;
        await UpdateUkshUI(true, skipLog: false);
      }
    }

    /// <summary>
    /// Обработчик кнопки "Сброс устройства".
    /// </summary>
    private async void BtnUkshReset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (!isUkshInitialized) return;
      await ResetUkshDevice();
    }

    /// <summary>
    /// Обработчик кнопки "ЗАПУСТИТЬ" (по аналогии с MkrControl).
    /// </summary>
    private async void BtnUkshStart_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (!isUkshInitialized)
      {
        await ShowMessageAsync("Устройство не выбрано!");
        return;
      }

      // Пример логики: при "ЗАПУСТИТЬ" подключаем шину, при "ОСТАНОВИТЬ" отключаем
      isShinaConnected = !isShinaConnected;
      if (isShinaConnected)
      {
        TbSearchRelays.IsEnabled = false;
        IcRelays.IsEnabled = false;
        BtnUkshReset.IsEnabled = false;
        BtnUkshStart.Content = "ОСТАНОВИТЬ";
        CmbUkshInit.IsEnabled = false;
        await ShowMessageAsync("Подключение шины (запуск)");
      }
      else
      {
        TbSearchRelays.IsEnabled = true;
        IcRelays.IsEnabled = true;
        BtnUkshReset.IsEnabled = true;
        BtnUkshStart.Content = "ЗАПУСТИТЬ";
        CmbUkshInit.IsEnabled = true;
        await ShowMessageAsync("Отключение шины (останов)");
      }
    }

    /// <summary>
    /// Обработчик изменения текста в поле поиска реле.
    /// </summary>
    private void TbSearchRelays_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (!isUkshInitialized) return;
      string text = TbSearchRelays.Text.Trim();
      FilterRelays(text);
    }

    /// <summary>
    /// Обработчик нажатия на кнопку реле (BtnRelay_PreviewMouseDown).
    /// </summary>
    private async void BtnRelay_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (!isUkshInitialized) return;

      if (sender is Button btn && btn.DataContext is Mode.Models.RelayModel relay)
      {
        relay.IsOn = !relay.IsOn;
        await ShowMessageAsync(relay.IsOn
            ? $"Реле {relay.RelayNum} включено"
            : $"Реле {relay.RelayNum} отключено");
      }
    }

    /// <summary>
    /// Асинхронный метод вывода сообщения в InvokeRichTextBoxUI.
    /// </summary>
    private Task ShowMessageAsync(string text)
    {
      protocolTextBox?.ShowMessageAsync($"{text}\n");
      protocolTextBox?.ScrollToEnd();
      return Task.CompletedTask;
    }
  }
}
