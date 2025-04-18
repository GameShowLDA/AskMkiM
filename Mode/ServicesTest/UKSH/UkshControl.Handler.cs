using System.Windows.Controls;
using System.Windows.Input;
using Mode.Models;

namespace Mode.ServicesTest.UKSH
{
  /// <summary>
  /// Реализация обработчиков событий для элемента управления UKSH.
  /// </summary>
  public partial class UkshControl
  {
    /// <summary>
    /// Обрабатывает изменение выбранного элемента в ComboBox (CmbUkshInit).
    /// Если шина уже подключена, изменение не производится. При выборе пустого элемента происходит сброс устройства.
    /// </summary>
    /// <param name="sender">Источник события, ожидается ComboBox.</param>
    /// <param name="e">Аргументы события изменения выбора.</param>
    private async void CmbUkshInit_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
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
    /// Обрабатывает нажатие на кнопку "Сброс устройства".
    /// Если устройство не инициализировано, действие не выполняется.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void BtnUkshReset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (!isUkshInitialized)
        return;
      await ResetUkshDevice();
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку "ЗАПУСТИТЬ"/"ОСТАНОВИТЬ".
    /// Переключает состояние подключения шины, обновляя элементы управления и выводя сообщение.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void BtnUkshStart_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (!isUkshInitialized)
      {
        await ShowMessageAsync("Устройство не выбрано!");
        return;
      }

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
    /// Обрабатывает изменение текста в поле поиска реле.
    /// Выполняет фильтрацию списка реле по введенному тексту.
    /// </summary>
    /// <param name="sender">Источник события, ожидается TextBox.</param>
    /// <param name="e">Аргументы события изменения текста.</param>
    private void TbSearchRelays_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (!isUkshInitialized)
        return;
      string text = TbSearchRelays.Text.Trim();
      FilterRelays(text);
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку реле.
    /// Переключает состояние реле и выводит соответствующее сообщение.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button с привязанной моделью RelayModel.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void BtnRelay_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (!isUkshInitialized)
        return;

      if (sender is Button btn && btn.DataContext is RelayModel relay)
      {
        relay.IsOn = !relay.IsOn;
        await ShowMessageAsync(relay.IsOn
            ? $"Реле {relay.RelayNum} включено"
            : $"Реле {relay.RelayNum} отключено");
      }
    }

    /// <summary>
    /// Асинхронно выводит сообщение в лог (например, в protocolTextBox).
    /// </summary>
    /// <param name="text">Текст сообщения.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private Task ShowMessageAsync(string text)
    {
      protocolTextBox?.ShowMessageAsync(new Utilities.Models.ShowMessageModel(text));
      protocolTextBox?.ScrollToEnd();
      return Task.CompletedTask;
    }
  }
}