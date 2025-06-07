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
    /// Этот метод будет вызван из MkrContent, когда в ComboBox выбрано новое устройство.
    /// </summary>
    /// <param name="selectedString">
    /// Строка в формате "шасси.номер" или "<пусто>". 
    /// Если null или пусто, считаем, что пользователь сбросил выбор.
    /// </param>
    public async Task HandleSerialSelectionAsync(string selectedString)
    {
      if (currentDevice != null) await ResetMkrDevice();
      // Если родитель передал null или "<пусто>" — значит, надо сбросить устройство.
      if (string.IsNullOrEmpty(selectedString) || selectedString == "<пусто>")
      {
        if (currentDevice != null)
        {
          // Показываем сообщение об отключении
          await ProtocolSelfCheckControl.ShowMessageAsync(
              new ShowMessageModel("[ОТКЛЮЧЕНИЕ]", goodText.TitleColor));
          // сбрасываем
          //await currentDevice.ConnectableManager.DisconnectAsync();
        }
        currentDevice = null;

        // очищаем коллекцию точек
        points.Clear();
        pointsView.Refresh();

        UpdateMkrUI(false);
        return;
      }

      // Разделяем строку "шасси.номер"
      var parts = selectedString.Split('.');
      if (parts.Length != 2)
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel("[ОШИБКА]", errorText.TitleColor, "некорректный формат выбора!"));
        return;
      }

      // Ищем в списке _devices нужное устройство
      if (_devices == null)
      {
        // Если _devices ещё не инициализирован (маловероятно),
        // заново подгрузим его
        _devices = _relayService.GetAllDevices();
      }

      currentDevice = _devices
          .FirstOrDefault(d =>
              d.NumberChassis.ToString() == parts[0]
              && d.Number.ToString() == parts[1]);

      if (currentDevice == null)
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel("[ОШИБКА]", errorText.TitleColor, "не удалось найти устройство!"));
        return;
      }

      // Запускаем инициализацию точек
      InitializePoints();

      // Включаем UI (кнопка Reset, радио-кнопки и т. д.)
      UpdateMkrUI(true);

      // Инициализируем устройство
      await currentDevice.ConnectableManager.InitializeAsync();
      await ProtocolSelfCheckControl.ShowMessageAsync(
          new ShowMessageModel($"[ИНИЦИАЛИЗАЦИЯ БК {currentDevice.NumberChassis}.{currentDevice.Number}]", goodText.TitleColor));

      await currentDevice.ConnectableManager.ConnectAsync();
      await ProtocolSelfCheckControl.ShowMessageAsync(
          new ShowMessageModel("[ПОДКЛЮЧЕНИЕ]", goodText.TitleColor));
    }


    /// <summary>
    /// Вызывается, когда юзер нажал «Сброс устройства» в MkrContent.
    /// Здесь мы просто делегируем на уже имеющийся приватный метод ResetMkrDevice().
    /// </summary>
    public async Task HandleResetDeviceAsync()
    {
      // Можете добавить любую свою валидацию, 
      // но просто вызываем ResetMkrDevice(), который у вас уже реализован.
      await ResetMkrDevice();
    }


    /// <summary>
    /// При любом изменении текста в строке «Поиск» в MkrContent.
    /// Родитель фильтрует коллекцию точек.
    /// </summary>
    /// <param name="searchText">Текст, который ввёл пользователь.</param>
    public void HandleSearchTextChanged(string searchText)
    {
      if (string.IsNullOrWhiteSpace(searchText))
      {
        pointsView.Filter = null;
      }
      else
      {
        pointsView.Filter = obj =>
        {
          if (obj is MkrPointModel p)
            return p.PointNumber.ToString().Contains(searchText);
          return false;
        };
      }
      pointsView.Refresh();
    }


    /// <summary>
    /// Если пользователь нажал на кнопку-точку, мы просто запомним, 
    /// что точка «отмечена» (UI-часть MkrContent установила свойство p.changeFlag = true),
    /// но реальный вызов ConnectRelay/DisconnectRelay произойдёт после MouseLeave контекстного меню.
    /// Поэтому здесь можно, например, только сохранить текущую модель, 
    /// либо сделать ничегонезависимое, если не нужна дополнительная логика.
    /// </summary>
    public void HandlePointClick(MkrPointModel point)
    {
      // Обычно в старом коде вы здесь ничего не делали,
      // потому что реальное переключение шины было в ContextMenu_MouseLeave.
      // Но если вам нужно сразу какое-то действие, то можно добавить.
    }

    /// <summary>
    /// Когда уходит мышь с контекстного меню, 
    /// нужно разобраться, какие флаги A/B у точки, 
    /// и вызвать ConnectRelay/DisconnectRelay, а потом ShowMessageAsync.
    /// </summary>
    /// <param name="ctxMenu">Контекстное меню, из которого ушла мышь.</param>
    public async Task HandleContextMenuMouseLeaveAsync(ContextMenu ctxMenu)
    {
      if (ctxMenu == null) return;
      // Найдём кнопку, на которой было контекстное меню:
      if (ctxMenu.PlacementTarget is Button btn
          && btn.DataContext is MkrPointModel point)
      {
        ctxMenu.IsOpen = false;

        if (!point.changeFlag)
          return; // если флаг не выставлен, ничего не делаем

        int buttonNumber = int.Parse(btn.Content.ToString());

        // Логика подключения/отключения точек из вашего кода:
        if (point.A)
          await currentDevice.PointManager.ConnectRelayAsync(DeviceEnum.BusPoint.A, buttonNumber);
        else
          await currentDevice.PointManager.DisconnectRelayAsync(DeviceEnum.BusPoint.A, buttonNumber);

        if (point.B)
          await currentDevice.PointManager.ConnectRelayAsync(DeviceEnum.BusPoint.B, buttonNumber);
        else
          await currentDevice.PointManager.DisconnectRelayAsync(DeviceEnum.BusPoint.B, buttonNumber);

        // Выводим сообщение, в зависимости от комбинации флагов:
        if (point.A && point.B)
          await ProtocolSelfCheckControl.ShowMessageAsync(
              new ShowMessageModel($"Точка {buttonNumber} подключена к A и B."));
        else if (point.A)
          await ProtocolSelfCheckControl.ShowMessageAsync(
              new ShowMessageModel($"Точка {buttonNumber} подключена к A."));
        else if (point.B)
          await ProtocolSelfCheckControl.ShowMessageAsync(
              new ShowMessageModel($"Точка {buttonNumber} подключена к B."));
        else
          await ProtocolSelfCheckControl.ShowMessageAsync(
              new ShowMessageModel($"Точка {buttonNumber} отключена."));

        // Сбросим флаг, чтобы при повторном открытии меню он не срабатывал зря:
        point.ResetChangeFlag();
      }
    }

    /// <summary>
    /// При вызове из MkrContent означает, что юзер нажал на RadioButton шины.
    /// <paramref name="radioName"/> — это, например, "RbAB1", "RbAB2" или "RbOff".
    /// </summary>
    public async Task HandleBusRadioClickAsync(RadioButton newBus)
    {
      // Если уже есть текущая шина, отключаем её
      if (currentBus != null && currentBus != _content.RbOff)
      {
        await DisconnectBusGroup();
        await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel($"Группа шин {currentBus.Content} отключена"));
      }

      // Отмечаем старую шину как доступную
      if (currentBus != null)
      {
        currentBus.IsHitTestVisible = true;
        currentBus.IsChecked = false;
      }

      if (newBus == null)
      {
        // Если что-то пошло не так, сообщение об ошибке
        await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel("[ОШИБКА]", errorText.TitleColor, "не удалось найти RadioButton!"));
        return;
      }

      // Ставим новую шину как currentBus
      currentBus = newBus;
      if (currentBus != _content.RbOff)
        await ConnectBusGroup();

      // Блокируем её (чтобы нельзя было снова нажать до разблокировки)
      currentBus.IsHitTestVisible = false;
      currentBus.IsChecked = true;

      // Выводим сообщение о состоянии шин
      string busState = (newBus.Name == "RbOff")
          ? "Все группы шин отключены"
          : $"Группа шин {currentBus.Content} подключена";
      await ProtocolSelfCheckControl.ShowMessageAsync(
          new ShowMessageModel(busState));
    }
  }
}