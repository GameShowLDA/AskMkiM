using NewCore.Enum;
using Utilities.Models;

namespace Mode.ServicesTest.MKR
{
  /// <summary>
  /// Управляет состоянием устройства МКР, включая его инициализацию, сброс параметров и обновление пользовательского интерфейса.
  /// </summary>
  public partial class MkrControl
  {
    /// <summary>
    /// Отключает группу шин на текущем устройстве в зависимости от имени выбранного переключателя.
    /// </summary>
    /// <remarks>
    /// Проверяется свойство <c>currentBus.Name</c>, и для каждого поддерживаемого значения
    /// выполняется асинхронный вызов метода <c>DisconnectBusAsync</c> с соответствующим значением <see cref="DeviceEnum.SwitchingBus"/>.
    /// </remarks>
    private async Task DisconnectBusGroup()
    {
      switch (currentBus.Name)
      {
        case "RbAB1":
          await currentDevice.BusManager.DisconnectBusAsync(DeviceEnum.SwitchingBus.AB1, false);
          break;
        case "RbAB2":
          await currentDevice.BusManager.DisconnectBusAsync(DeviceEnum.SwitchingBus.AB2, false);
          break;
        case "RbAB3":
          await currentDevice.BusManager.DisconnectBusAsync(DeviceEnum.SwitchingBus.AB3, false);
          break;
        case "RbAB4":
          await currentDevice.BusManager.DisconnectBusAsync(DeviceEnum.SwitchingBus.AB4, false);
          break;
      }
    }

    /// <summary>
    /// Подключает группу шин на текущем устройстве в зависимости от имени выбранного переключателя.
    /// </summary>
    /// <remarks>
    /// Аналогично <see cref="DisconnectBusGroup"/>, но вызывает <c>ConnectBusAsync</c> для указанной шины.
    /// </remarks>
    private async Task ConnectBusGroup()
    {
      switch (currentBus.Name)
      {
        case "RbAB1":
          await currentDevice.BusManager.ConnectBusAsync(DeviceEnum.SwitchingBus.AB1, false);
          break;
        case "RbAB2":
          await currentDevice.BusManager.ConnectBusAsync(DeviceEnum.SwitchingBus.AB2, false);
          break;
        case "RbAB3":
          await currentDevice.BusManager.ConnectBusAsync(DeviceEnum.SwitchingBus.AB3, false);
          break;
        case "RbAB4":
          await currentDevice.BusManager.ConnectBusAsync(DeviceEnum.SwitchingBus.AB4, false);
          break;
      }
    }

    /// <summary>
    /// Сбрасывает устройство, приводя его к начальному состоянию.
    /// Осуществляется отключение подключения, сброс состояния радиокнопок и точек.
    /// </summary>
    private async Task ResetMkrDevice()
    {
      await protocolTextBox.ShowMessageAsync(new ShowMessageModel("[СБРОС МОДУЛЯ]", goodText.TitleColor));

      currentBus.IsChecked = false;
      currentBus.IsHitTestVisible = true;
      currentBus = RbOff;
      RbOff.IsChecked = true;
      RbOff.IsHitTestVisible = false;

      // Сброс состояния точек с использованием DeferRefresh для минимизации обновлений UI.
      using (pointsView.DeferRefresh())
      {
        foreach (var point in points)
        {
          point.A = false;
          point.B = false;
        }
      }

      await currentDevice.StateManager.ResetAsync();
    }

    /// <summary>
    /// Включает или отключает доступность элементов управления в зависимости от состояния устройства.
    /// Обновляет состояние кнопок, поля поиска, списка точек и ComboBox.
    /// </summary>
    /// <param name="enable">
    /// Если <c>true</c>, элементы управления становятся доступными (устройство инициализировано);
    /// если <c>false</c>, они отключаются.
    /// </param>
    private void UpdateMkrUI(bool enable)
    {
      BtnMkrReset.IsEnabled = enable;

      ToggleRadioButtonState(enable);

      SearchBox.IsEnabled = enable;
    }

    /// <summary>
    /// Переключает доступность группы RadioButton для шины A и шины B.
    /// </summary>
    /// <param name="enable">Если true, все RadioButton становятся доступными.</param>
    private void ToggleRadioButtonState(bool enable)
    {
      RbAB1.IsEnabled = enable;
      RbAB2.IsEnabled = enable;
      RbAB3.IsEnabled = enable;
      RbAB4.IsEnabled = enable;
      RbOff.IsEnabled = enable;
    }
  }
}