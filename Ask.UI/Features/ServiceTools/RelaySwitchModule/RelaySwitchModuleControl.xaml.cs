using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.LogLib;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.Features.ServiceTools.RelaySwitchModule
{
  /// <summary>
  /// Предоставляет сервисное управление модулями коммутации реле.
  /// </summary>
  public partial class RelaySwitchModuleControl : UserControl
  {
    private readonly Func<Task<IReadOnlyList<IRelaySwitchModule>>> modulesProvider;
    private bool operationInProgress;

    /// <summary>
    /// Инициализирует панель управления модулями коммутации реле.
    /// </summary>
    /// <param name="modulesProvider">Функция получения модулей первого шасси.</param>
    public RelaySwitchModuleControl(Func<Task<IReadOnlyList<IRelaySwitchModule>>> modulesProvider)
    {
      this.modulesProvider = modulesProvider ?? throw new ArgumentNullException(nameof(modulesProvider));
      InitializeComponent();
      PointBusComboBox.ItemsSource = Enum.GetValues<BusPoint>();
      GroupBusComboBox.ItemsSource = Enum.GetValues<BusPoint>();
      SwitchingBusComboBox.ItemsSource = Enum.GetValues<SwitchingBus>();
      PointBusComboBox.SelectedIndex = 0;
      GroupBusComboBox.SelectedIndex = 0;
      SwitchingBusComboBox.SelectedIndex = 0;
      Loaded += RelaySwitchModuleControl_Loaded;
    }

    private IRelaySwitchModule CurrentModule =>
      ModuleComboBox.SelectedItem as IRelaySwitchModule
      ?? throw new InvalidOperationException("Выберите модуль МКР.");

    private async void RelaySwitchModuleControl_Loaded(object sender, RoutedEventArgs e)
    {
      Loaded -= RelaySwitchModuleControl_Loaded;
      await LoadModulesAsync();
    }

    private async Task LoadModulesAsync()
    {
      try
      {
        var modules = await modulesProvider();
        ModuleComboBox.ItemsSource = modules;
        ModuleComboBox.SelectedIndex = modules.Count > 0 ? 0 : -1;
        UpdateStatus();
      }
      catch (Exception exception)
      {
        ModuleComboBox.ItemsSource = null;
        StatusText.Text = "Не удалось загрузить МКР. Подробности записаны в консоль.";
        StatusIndicator.Background = Brushes.IndianRed;
        ReportError("загрузка модулей", exception);
      }
    }

    private void UpdateStatus()
    {
      if (ModuleComboBox.SelectedItem is not IRelaySwitchModule module)
      {
        StatusText.Text = "МКР не найдены в конфигурации первого шасси.";
        StatusIndicator.Background = Brushes.IndianRed;
        RefreshConnections();
        return;
      }

      StatusText.Text = $"{module.Name}, устройство №{module.Number}, точек: {module.PointCount}";
      StatusIndicator.Background = Brushes.MediumSeaGreen;
      RefreshConnections();
    }

    private async Task ExecuteAsync(string operation, Func<IRelaySwitchModule, Task<bool>> action)
    {
      if (operationInProgress)
      {
        return;
      }

      try
      {
        operationInProgress = true;
        IsEnabled = false;
        if (!await action(CurrentModule))
        {
          throw new InvalidOperationException("Устройство вернуло отрицательный результат.");
        }

        LoggerUtility.LogInformation($"МКР — {operation}: выполнено.", isDeviceLog: true);
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

    private void RefreshConnections()
    {
      if (ModuleComboBox.SelectedItem is not IRelaySwitchModule module)
      {
        ConnectionsList.ItemsSource = null;
        NoConnectionsText.Visibility = Visibility.Visible;
        return;
      }

      var items = module.PointManager.GetConnectedPoints()
        .Select(point => $"Точка {point.PointNumber} — шина {point.Bus}")
        .Concat(module.BusManager.GetConnectedBuses()
          .Where(bus => bus.IsConnected)
          .Select(bus => $"Шина {bus.Bus} подключена"))
        .ToList();
      ConnectionsList.ItemsSource = items;
      NoConnectionsText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static int Number(TextBox textBox, string name)
    {
      if (!int.TryParse(textBox.Text, out int value) || value <= 0)
      {
        throw new InvalidOperationException($"Укажите корректный {name}.");
      }

      return value;
    }

    private BusPoint PointBus(ComboBox box) => box.SelectedItem is BusPoint bus
      ? bus
      : throw new InvalidOperationException("Выберите шину точки.");

    private static void ReportError(string operation, Exception exception) =>
      LoggerUtility.LogError($"МКР — {operation}: {exception.Message}", isDeviceLog: true);

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadModulesAsync();
    private void ModuleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateStatus();
    private async void ConnectPointButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("подключение точки", m => m.PointManager.ConnectRelayAsync(PointBus(PointBusComboBox), Number(PointNumberTextBox, "номер точки")));
    private async void DisconnectPointButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение точки", m => m.PointManager.DisconnectRelayAsync(PointBus(PointBusComboBox), Number(PointNumberTextBox, "номер точки")));
    private async void ConnectPointVerifiedButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("подключение точки с проверкой", m => m.PointManager.ConnectRelayVerifiedAsync(PointBus(PointBusComboBox), Number(PointNumberTextBox, "номер точки")));
    private async void DisconnectPointVerifiedButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение точки с проверкой", m => m.PointManager.DisconnectRelayVerifiedAsync(PointBus(PointBusComboBox), Number(PointNumberTextBox, "номер точки")));
    private async void MovePointButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("переподключение точки", m => m.PointManager.ConnectingPointToNewBus(PointBus(PointBusComboBox), Number(PointNumberTextBox, "номер точки")));
    private async void ConnectGroupButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("подключение диапазона", m => m.PointManager.ConnectRelayGroupAsync(PointBus(GroupBusComboBox), Number(FirstPointTextBox, "начальную точку"), Number(LastPointTextBox, "конечную точку")));
    private async void DisconnectGroupButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение диапазона", m => m.PointManager.DisconnectRelayGroupAsync(PointBus(GroupBusComboBox), Number(FirstPointTextBox, "начальную точку"), Number(LastPointTextBox, "конечную точку")));
    private async void ConnectMeterButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("включение измерителя", m => m.MeterManager.ConnectMeterAsync());
    private async void DisconnectMeterButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение измерителя", m => m.MeterManager.DisconnectMeterAsync());
    private async void DisconnectAllPointsButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение всех точек", m => m.PointManager.DisconnectingAllPoint());
    private async void DisconnectBusAPointsButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение точек шины A", m => m.PointManager.DisconnectingAllPointFromBusA());
    private async void DisconnectBusBPointsButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение точек шины B", m => m.PointManager.DisconnectingAllPointFromBusB());
    private async void ConnectBusButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("подключение шины", m => m.BusManager.ConnectBusAsync((SwitchingBus)SwitchingBusComboBox.SelectedItem));
    private async void DisconnectBusButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение шины", m => m.BusManager.DisconnectBusAsync((SwitchingBus)SwitchingBusComboBox.SelectedItem));
  }
}
