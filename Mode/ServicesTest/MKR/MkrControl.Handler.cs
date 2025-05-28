using Mode.Models;
using NewCore.Enum;
using System.Windows.Controls;
using System.Windows.Input;
using Utilities.Models;

namespace Mode.ServicesTest.MKR
{
  /// <summary>
  /// Реализует обработчики событий для управления устройством МКР.
  /// </summary>
  public partial class MkrControl
  {
    /// <summary>
    /// Текущая кнопка группы шин.
    /// </summary>
    private RadioButton currentBus;

    /// <summary>
    /// Обрабатывает изменение выбранного элемента в ComboBox (SerialNumComboBox).
    /// Если устройство уже подключено, изменение недопустимо. При выборе пустого элемента происходит сброс устройства.
    /// </summary>
    /// <param name="sender">Источник события, ожидается ComboBox.</param>
    /// <param name="e">Аргументы события изменения выбора.</param>
    private async void SerialNumComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

      var selectedSerial = (SerialNumComboBox.SelectedItem as string).Split('.');

      if (currentDevice != null) await ResetMkrDevice();

      if (selectedSerial[0] == "<пусто>")
      {
        currentDevice = null;

        points.Clear();
        pointsView.Refresh();

        UpdateMkrUI(false);
        return;
      }

      currentDevice = _devices
                      .FirstOrDefault(
                      d => 
                      d.NumberChassis.ToString() == selectedSerial[0]
                      && d.Number.ToString() == selectedSerial[1]
                      );

      if (currentDevice == null)
      {
        await protocolTextBox.ShowMessageAsync(new ShowMessageModel("[ОШИБКА]: ", errorText.TitleColor, "не удалось найти устройство!"));
        return;
      }

      InitializePoints();

      UpdateMkrUI(true);

      await currentDevice.StateManager.Initialize();
      await protocolTextBox.ShowMessageAsync(new ShowMessageModel($"[ИНИЦИАЛИЗАЦИЯ БК {currentDevice.NumberChassis}.{currentDevice.Number}]", goodText.TitleColor));
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
    /// Здесь реализовано управление подключения точек к шинам.
    /// </summary>
    /// <param name="sender">Источник события, ожидается ContextMenu.</param>
    /// <param name="e">Аргументы события ухода мыши.</param>
    private async void ContextMenu_MouseLeave(object sender, MouseEventArgs e)
    {
      ContextMenu ctxMenu = sender as ContextMenu;
      Button btn = ctxMenu.PlacementTarget as Button;
      MkrPointModel point = btn.DataContext as MkrPointModel;

      ctxMenu.IsOpen = false;

      if (!point.changeFlag) return;

      int buttonNumber = int.Parse(btn.Content.ToString());

      if (point.A) await currentDevice.PointManager.ConnectRelayAsync(DeviceEnum.BusPoint.A, buttonNumber);
      else if (!point.A) await currentDevice.PointManager.DisconnectRelayAsync(DeviceEnum.BusPoint.A, buttonNumber);

      if (point.B) await currentDevice.PointManager.ConnectRelayAsync(DeviceEnum.BusPoint.B, buttonNumber);
      else if (!point.B) await currentDevice.PointManager.DisconnectRelayAsync(DeviceEnum.BusPoint.B, buttonNumber);

      if (point.A && point.B) await protocolTextBox.ShowMessageAsync(new ShowMessageModel($"Точка {buttonNumber} подключена к A и B."));
      else if (point.A) await protocolTextBox.ShowMessageAsync(new ShowMessageModel($"Точка {buttonNumber} подключена к A."));
      else if (point.B) await protocolTextBox.ShowMessageAsync(new ShowMessageModel($"Точка {buttonNumber} подключена к B."));
      else await protocolTextBox.ShowMessageAsync(new ShowMessageModel($"Точка {buttonNumber} отключена."));

      point.ResetChangeFlag();
    }

    /// <summary>
    /// Обрабатывает нажатие на RadioButton для выбора шины.
    /// Выводит сообщение о текущем состоянии шины, основываясь на имени элемента.
    /// Здесь реализовано управление подключения шин.
    /// </summary>
    /// <param name="sender">Источник события, ожидается RadioButton.</param>
    /// <param name="e">Аргументы события нажатия мыши.</param>
    private async void RadioButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      RadioButton rb = sender as RadioButton;

      if (currentBus != RbOff)
      { 
        await DisconnectBusGroup();
        await protocolTextBox.ShowMessageAsync(new ShowMessageModel($"Группа шин {currentBus.Content} отключена"));
      }
      currentBus.IsHitTestVisible = true;
      currentBus.IsChecked = false;

      currentBus = rb;
      if (currentBus != RbOff) await ConnectBusGroup();
      currentBus.IsHitTestVisible = false;
      currentBus.IsChecked = true;

      string busState = rb.Name == "RbOff" ? "Все группы шин отключены" : $"Группа шин {rb.Content} подключена";
      await protocolTextBox.ShowMessageAsync(new ShowMessageModel(busState));
    }
  }
}