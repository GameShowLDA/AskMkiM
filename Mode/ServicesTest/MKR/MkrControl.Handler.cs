using Mode.Models;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mode.ServicesTest.MKR
{
  /// <summary>
  /// Реализует обработчики событий для управления устройством МКР.
  /// </summary>
  public partial class MkrControl
  {
    /// <summary>
    /// Обрабатывает изменение выбранного элемента в ComboBox (SerialNumComboBox).
    /// Если устройство уже подключено, изменение недопустимо. При выборе пустого элемента происходит сброс устройства.
    /// </summary>
    /// <param name="sender">Источник события, ожидается ComboBox.</param>
    /// <param name="e">Аргументы события изменения выбора.</param>
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
        if (isMkrInitialized)
        {
          await ResetMkrDevice();
          await ShowMessageAsync("Устройство отключено");
        }

        isMkrInitialized = false;
        currentDeviceName = string.Empty;
        await UpdateMkrUI(false, skipLog: true);
      }
      else
      {
        if (isMkrInitialized)
        {
          await ResetMkrDevice();
        }
        isMkrInitialized = true;
        currentDeviceName = selectedItem;
        await UpdateMkrUI(true, skipLog: false);
      }
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку "Сброс устройства" (BtnMkrReset).
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void BtnMkrReset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      await ResetMkrDevice();
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку "ЗАПУСТИТЬ"/"ОСТАНОВИТЬ" (BtnConnect).
    /// После попытки подключения обновляются состояния радиокнопок, списка точек и ComboBox.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void BtnConnect_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      await AttemptDeviceConnectionAsync();

      ToggleRadioButtonState(!isConnected);
      PointsListBox.IsEnabled = !isConnected;
      SerialNumComboBox.IsEnabled = !isConnected;
      BtnMkrReset.IsEnabled = !isConnected;
    }

    /// <summary>
    /// Обрабатывает изменение текста в поле поиска точек.
    /// Если введенная строка пуста, фильтр снимается, иначе выполняется фильтрация по номеру точки.
    /// </summary>
    /// <param name="sender">Источник события, ожидается TextBox.</param>
    /// <param name="e">Аргументы события изменения текста.</param>
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      string searchText = SearchBox.Text.Trim();

      if (string.IsNullOrEmpty(searchText))
      {
        pointsView.Filter = null;
      }
      else
      {
        pointsView.Filter = obj =>
        {
          if (obj is MkrPointModel p)
          {
            return p.PointNumber.ToString().Contains(searchText);
          }
          return false;
        };
      }
      pointsView.Refresh();
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку точки.
    /// Здесь реализуйте логику подключения точки к шине.
    /// </summary>
    /// <param name="sender">Источник события, ожидается Button с привязанной моделью MkrPointModel.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private void Point_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is Button btn && btn.DataContext is MkrPointModel point)
      {
        // Логика подключения точки к шине (реализуйте по необходимости)
      }
    }

    /// <summary>
    /// Обрабатывает событие ухода мыши с ContextMenu.
    /// Выводит сообщение о состоянии подключения точки, основываясь на данных модели, и закрывает меню.
    /// </summary>
    /// <param name="sender">Источник события, ожидается ContextMenu.</param>
    /// <param name="e">Аргументы события ухода мыши.</param>
    private async void ContextMenu_MouseLeave(object sender, MouseEventArgs e)
    {
      if (sender is ContextMenu ctxMenu)
      {
        if (ctxMenu.PlacementTarget is Button btn)
        {
          string buttonNumber = btn.Content?.ToString() ?? "{неизвестно}";

          if (btn.DataContext is MkrPointModel point)
          {
            if (point.A && point.B)
              await ShowMessageAsync($"Точка {buttonNumber} подключена к A и B.");
            else if (point.A)
              await ShowMessageAsync($"Точка {buttonNumber} подключена к A.");
            else if (point.B)
              await ShowMessageAsync($"Точка {buttonNumber} подключена к B.");
            else
              await ShowMessageAsync($"Точка {buttonNumber} отключена.");
          }
        }
        ctxMenu.IsOpen = false;
      }
    }

    /// <summary>
    /// Обрабатывает нажатие на RadioButton для выбора шины.
    /// Выводит сообщение о текущем состоянии шины, основываясь на имени элемента.
    /// </summary>
    /// <param name="sender">Источник события, ожидается RadioButton.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void RadioButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is RadioButton rb)
      {
        string busState = rb.Name switch
        {
          "RbOffA" => "Шина A отключена",
          "RbOffB" => "Шина B отключена",
          _ => $"Шина {rb.Name} подключена"
        };

        await ShowMessageAsync(busState);
      }
    }

    /// <summary>
    /// Переключает доступность группы RadioButton для шины A и шины B.
    /// </summary>
    /// <param name="enable">Если true, все RadioButton становятся доступными.</param>
    private void ToggleRadioButtonState(bool enable)
    {
      RbA1.IsEnabled = enable;
      RbA2.IsEnabled = enable;
      RbA3.IsEnabled = enable;
      RbA4.IsEnabled = enable;
      RbOffA.IsEnabled = enable;

      RbB1.IsEnabled = enable;
      RbB2.IsEnabled = enable;
      RbB3.IsEnabled = enable;
      RbB4.IsEnabled = enable;
      RbOffB.IsEnabled = enable;
    }

    /// <summary>
    /// Выводит сообщение в элемент протокола (protocolTextBox).
    /// </summary>
    /// <param name="text">Текст сообщения.</param>
    /// <returns>Задача, представляющая завершение асинхронной операции.</returns>
    private Task ShowMessageAsync(string text)
    {
      if (protocolTextBox != null)
      {
        protocolTextBox.ShowMessageAsync($"{text}\n");
        protocolTextBox.ScrollToEnd();
      }
      return Task.CompletedTask;
    }
  }
}