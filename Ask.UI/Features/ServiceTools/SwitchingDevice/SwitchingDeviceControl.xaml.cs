using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.LogLib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.Features.ServiceTools.SwitchingDevice
{
  /// <summary>
  /// Предоставляет визуальное управление сервисными функциями устройства коммутации шин.
  /// </summary>
  public partial class SwitchingDeviceControl : UserControl
  {
    private readonly Func<Task<ISwitchingDevice?>> deviceProvider;
    private ISwitchingDevice? device;
    private bool operationInProgress;

    /// <summary>
    /// Инициализирует элемент управления устройством коммутации шин.
    /// </summary>
    /// <param name="deviceProvider">Функция получения настроенного устройства коммутации шин.</param>
    public SwitchingDeviceControl(Func<Task<ISwitchingDevice?>> deviceProvider)
    {
      this.deviceProvider = deviceProvider
        ?? throw new ArgumentNullException(nameof(deviceProvider));

      InitializeComponent();
      BusComboBox.ItemsSource = Enum.GetValues<SwitchingBusNew>();
      Loaded += SwitchingDeviceControl_Loaded;
    }

    private async void SwitchingDeviceControl_Loaded(object sender, RoutedEventArgs e)
    {
      Loaded -= SwitchingDeviceControl_Loaded;
      await LoadDeviceAsync();
    }

    private async Task LoadDeviceAsync()
    {
      try
      {
        device = await deviceProvider();
        DeviceStatusText.Text = device is null
          ? "УКШ не найдено. Проверьте конфигурацию первого шасси."
          : $"{device.Name}, шасси 1, устройство №{device.Number}";
        DeviceStatusIndicator.Background = device is null
          ? Brushes.IndianRed
          : Brushes.MediumSeaGreen;
        LoadSelfTestTypes();
        RefreshConnections();
      }
      catch (Exception exception)
      {
        device = null;
        ReportError("загрузка устройства", exception);
        DeviceStatusText.Text = "Не удалось загрузить УКШ. Подробности записаны в консоль.";
        DeviceStatusIndicator.Background = Brushes.IndianRed;
      }
    }

    private async Task ExecuteAsync(string operation, Func<ISwitchingDevice, Task<bool>> action)
    {
      if (operationInProgress)
      {
        return;
      }

      try
      {
        operationInProgress = true;
        IsEnabled = false;

        var currentDevice = device
          ?? throw new InvalidOperationException(
            "УКШ не найдено. Проверьте конфигурацию оборудования.");

        bool result = await action(currentDevice);
        if (!result)
        {
          throw new InvalidOperationException("Устройство вернуло отрицательный результат.");
        }

        LoggerUtility.LogInformation($"УКШ — {operation}: выполнено.", isDeviceLog: true);
        RefreshConnections();
      }
      catch (Exception exception)
      {
        ReportError(operation, exception);
      }
      finally
      {
        IsEnabled = true;
        operationInProgress = false;
      }
    }

    private SwitchingBusNew GetSelectedBus()
    {
      return BusComboBox.SelectedItem is SwitchingBusNew bus
        ? bus
        : throw new InvalidOperationException("Выберите шину.");
    }

    private static int GetPositiveNumber(TextBox textBox, string valueName)
    {
      if (!int.TryParse(textBox.Text, out int number) || number < 0)
      {
        throw new InvalidOperationException($"Укажите корректный {valueName}.");
      }

      return number;
    }

    private void RefreshConnections()
    {
      var connections = device?.ConnectorManager.GetConnectedDevices()
        .Select(connection => $"{connection.device} — шина {connection.bus}")
        .ToList() ?? [];

      ConnectionsList.ItemsSource = connections;
      NoConnectionsText.Visibility = connections.Count == 0
        ? Visibility.Visible
        : Visibility.Collapsed;
    }

    private void LoadSelfTestTypes()
    {
      var testTypes = device?.SelfTestManager.GetSupportedTestTypes().ToList() ?? [];
      SelfTestTypeComboBox.ItemsSource = testTypes;
      SelfTestTypeComboBox.SelectedIndex = testTypes.Count > 0 ? 0 : -1;
    }

    private SwitchingDeviceTypeConnector GetSelectedSelfTestType()
    {
      return SelfTestTypeComboBox.SelectedItem is SwitchingDeviceTypeConnector testType
        ? testType
        : throw new InvalidOperationException("Выберите тип цепи самоконтроля.");
    }

    private int GetSelectedSelfTestContact()
    {
      return SelfTestContactComboBox.SelectedItem is int contact
        ? contact
        : throw new InvalidOperationException("Выберите контакт цепи самоконтроля.");
    }

    private void UpdateSelfTestCircuitName()
    {
      if (device is null
        || SelfTestTypeComboBox.SelectedItem is not SwitchingDeviceTypeConnector testType
        || SelfTestContactComboBox.SelectedItem is not int contact)
      {
        SelfTestCircuitNameText.Text = "Цепь не выбрана";
        return;
      }

      SelfTestCircuitNameText.Text =
        $"Цепь: {device.SelfTestManager.GetCircuitName(testType, contact)}";
    }

    private static void ReportError(string operation, Exception exception)
    {
      LoggerUtility.LogError(
        $"УКШ — {operation}: {exception.Message}",
        isDeviceLog: true);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadDeviceAsync();

    private void SelfTestTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (device is null
        || SelfTestTypeComboBox.SelectedItem is not SwitchingDeviceTypeConnector testType)
      {
        SelfTestContactComboBox.ItemsSource = null;
        UpdateSelfTestCircuitName();
        return;
      }

      var contacts = device.SelfTestManager.GetValidBusContacts(testType) ?? [];
      SelfTestContactComboBox.ItemsSource = contacts;
      SelfTestContactComboBox.SelectedIndex = contacts.Count > 0 ? 0 : -1;
      UpdateSelfTestCircuitName();
    }

    private void SelfTestContactComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
      UpdateSelfTestCircuitName();

    private async void ConnectSelfTestCircuitButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("подключение цепи самоконтроля", item =>
        item.SelfTestManager.ExecuteSelfTestAsync(
          CancellationToken.None,
          GetSelectedSelfTestType(),
          GetSelectedSelfTestContact(),
          action: 1));

    private async void DisconnectSelfTestCircuitButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение цепи самоконтроля", item =>
        item.SelfTestManager.ExecuteSelfTestAsync(
          CancellationToken.None,
          GetSelectedSelfTestType(),
          GetSelectedSelfTestContact(),
          action: 2));

    private async void ConnectMultimeterButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("подключение мультиметра", item => item.ConnectorManager.ConnectMultimeter(GetSelectedBus()));

    private async void DisconnectMultimeterButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение мультиметра", item => item.ConnectorManager.DisconnectMultimeter(GetSelectedBus()));

    private async void ConnectBreakdownTesterButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("подключение ППУ", item => item.ConnectorManager.ConnectBreakdownTester());

    private async void DisconnectBreakdownTesterButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение ППУ", item => item.ConnectorManager.DisconnectBreakdownTester());

    private async void ConnectBreakdownAndMeterButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync(
        "подключение ППУ и мультиметра",
        item => item.ConnectorManager.ConnectBreakdownTesterAndMultimeter());

    private async void DisconnectBreakdownAndMeterButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync(
        "отключение ППУ и мультиметра",
        item => item.ConnectorManager.DisconnectBreakdownTesterAndMultimeter());

    private async void ConnectAllBusesButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("подключение всех шин", item => item.ConnectorManager.ConnectAllBuses());

    private async void DisconnectAllBusesButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение всех шин", item => item.ConnectorManager.DisconnectAllBuses());

    private async void EnableDividerButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("включение делителя", item => item.ConnectorManager.EnableDivider());

    private async void DisableDividerButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение делителя", item => item.ConnectorManager.DisableDivider());

    private async void ConnectRelayButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("замыкание реле", item =>
      {
        int number = GetPositiveNumber(RelayNumberTextBox, "номер реле");
        return item.RelayManager.ConnectRelay(number);
      });
    }

    private async void DisconnectRelayButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("размыкание реле", item =>
      {
        int number = GetPositiveNumber(RelayNumberTextBox, "номер реле");
        return item.RelayManager.DisconnectRelay(number);
      });
    }

    private async void EnableRelayButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("общее включение реле", item => item.RelayManager.EnableRelay());

    private async void DisableRelayButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("общее отключение реле", item => item.RelayManager.DisableRelay());

    private async void ConnectResistorButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("подключение резистора", item =>
      {
        int number = GetPositiveNumber(ResistorNumberTextBox, "номер резистора");
        return item.ResistorManager.ConnectResistor(number.ToString());
      });
    }

    private async void DisconnectResistorButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("отключение резистора", item =>
      {
        int number = GetPositiveNumber(ResistorNumberTextBox, "номер резистора");
        return item.ResistorManager.DisconnectResistor(number.ToString());
      });
    }

    private async void ConnectCapacitorButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("подключение конденсатора", item =>
      {
        int number = GetPositiveNumber(CapacitorNumberTextBox, "номер конденсатора");
        return item.CapacitorManager.ConnectCapacitor(number);
      });
    }

    private async void DisconnectCapacitorButton_Click(object sender, RoutedEventArgs e)
    {
      await ExecuteAsync("отключение конденсатора", item =>
      {
        int number = GetPositiveNumber(CapacitorNumberTextBox, "номер конденсатора");
        return item.CapacitorManager.DisconnectCapacitor(number);
      });
    }
  }
}
