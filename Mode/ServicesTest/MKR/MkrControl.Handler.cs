using Mode.Models;
using Mode.ServicesTest.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mode.ServicesTest.MKR
{
  public partial class MkrControl
  {
    /// <summary>
    /// Обработчик изменения выбора в ComboBox (SerialNumComboBox).
    /// </summary>
    private async void SerialNumComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (isConnected)
      {
        await ShowMessageAsync("Нельзя сменить устройство: уже подключено!");
        return;
      }

      var selectedItem = SerialNumComboBox.SelectedItem as string;

      if (string.IsNullOrEmpty(selectedItem) || selectedItem == "<пусто>")
      {
        // Если выбран "<пусто>", сбрасываем устройство
        await ResetDeviceAsync();
        isMkrInitialized = false;
        currentDeviceName = string.Empty;
        await UpdateStateMKRAsync(false, skipLog: true);
        await ShowMessageAsync("Устройство отключено");
      }
      else
      {
        // Если выбран другой элемент, сбрасываем предыдущее состояние (если было) и разблокируем все кнопки
        if (isMkrInitialized)
        {
          await ResetDeviceAsync();
        }
        isMkrInitialized = true;
        currentDeviceName = selectedItem;
        await UpdateStateMKRAsync(true, skipLog: false);
      }
    }

    /// <summary>
    /// Обработчик нажатия кнопки "Сброс устройства" (BtnMkrReset).
    /// </summary>
    private async void BtnMkrReset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      await ResetDeviceAsync();
    }

    /// <summary>
    /// Обработчик нажатия кнопки "ЗАПУСТИТЬ"/"ОСТАНОВИТЬ" (BtnConnect).
    /// </summary>
    private async void BtnConnect_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      await AttemptDeviceConnectionAsync();

      // Радиокнопки и список точек блокируем при подключении
      ToggleRadioButtonState("BusA", !isConnected);
      ToggleRadioButtonState("BusB", !isConnected);
      PointsListBox.IsEnabled = !isConnected;
      SerialNumComboBox.IsEnabled = !isConnected;
      BtnMkrReset.IsEnabled = !isConnected;
    }

    /// <summary>
    /// Обработчик поиска по точкам.
    /// </summary>
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      string searchText = SearchBox.Text.Trim();
      points.Clear();

      if (string.IsNullOrEmpty(searchText))
      {
        // Показать все
        foreach (var p in allPoints) points.Add(p);
        return;
      }

      // Фильтрация
      if (int.TryParse(searchText, out _))
      {
        var filtered = allPoints.Where(p => p.PointNumber.ToString().Contains(searchText));
        foreach (var p in filtered) points.Add(p);
      }
    }

    /// <summary>
    /// Обработчик клика по кнопке точки (Point_PreviewMouseDown).
    /// </summary>
    private void Point_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is Button btn && btn.DataContext is MkrPointModel point)
      {
        // Логика подключения точки к шине
        // (у вас сейчас не описано, делайте по своему)
      }
    }

    /// <summary>
    /// Обработчик ухода мыши с ContextMenu (закрыть меню).
    /// </summary>
    private void ContextMenu_MouseLeave(object sender, MouseEventArgs e)
    {
      if (sender is ContextMenu ctxMenu)
        ctxMenu.IsOpen = false;
    }

    /// <summary>
    /// Обработчик нажатия RadioButton (BusA / BusB).
    /// </summary>
    private async void RadioButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is RadioButton rb)
      {
        string busState = rb.Name switch
        {
          "offA" => "Шина A отключена",
          "offB" => "Шина B отключена",
          _ => $"Шина {rb.Name} подключена"
        };

        await ShowMessageAsync(busState);
      }
    }

    /// <summary>
    /// Включение/выключение группы RadioButton
    /// </summary>
    private void ToggleRadioButtonState(string groupName, bool enable)
    {
      var radioButtons = this.FindVisualChildren<RadioButton>().Where(rb => rb.GroupName == groupName);
      foreach (var rb in radioButtons)
        rb.IsEnabled = enable;
    }

    /// <summary>
    /// Метод для вывода сообщений в InvokeRichTextBoxUI (заменяет Helpers.WriteInfo).
    /// </summary>
    private Task ShowMessageAsync(string text)
    {
      if (protocolTextBox != null)
      {
        // Метод AppendText - ваш, если он есть
        protocolTextBox.ShowMessageAsync($"{text}\n");
        protocolTextBox.ScrollToEnd();
      }
      return Task.CompletedTask;
    }
  }
}
