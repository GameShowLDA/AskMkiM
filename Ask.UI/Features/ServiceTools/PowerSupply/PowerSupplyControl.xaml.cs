using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.PowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Ask.LogLib;
using Ask.UI.Features.ProtocolNew.Execution;
using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Features.ServiceTools.PowerSupply;

/// <summary>
/// Управляет внешними источниками питания, выбранными из конфигурации оборудования.
/// </summary>
public partial class PowerSupplyControl : UserControl
{
  private readonly Func<CancellationToken, Task<IReadOnlyList<IPowerSupply>>> _provider;
  private readonly ExecutionRunGuard _runGuard = new();
  private CancellationTokenSource? _operationCancellation;
  private bool _busy;
  private bool _active;

  /// <summary>
  /// Создаёт панель сервисного управления внешними источниками питания.
  /// </summary>
  /// <param name="provider">Загрузка сохранённых источников питания.</param>
  public PowerSupplyControl(Func<CancellationToken, Task<IReadOnlyList<IPowerSupply>>> provider)
  {
    _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    InitializeComponent();
    Loaded += Control_Loaded;
    Unloaded += (_, _) => { _active = false; _operationCancellation?.Cancel(); };
  }

  private IPowerSupply Device => SourceComboBox.SelectedItem as IPowerSupply
    ?? throw new InvalidOperationException("Сначала сохраните и выберите источник питания.");

  private static void EnsureRoot()
  {
    if (RoleAuthorizationConfig.CurrentRole != RoleType.Root)
      throw new UnauthorizedAccessException("Сервисное управление доступно только root.");
  }

  private async void Control_Loaded(object sender, RoutedEventArgs e)
  {
    _active = true;
    await ExecuteAsync("Загрузка источников", token => LoadAsync(token: token));
  }

  private async Task LoadAsync(int selectedId = 0, CancellationToken token = default)
  {
    var devices = await _provider(token);
    if (!_active) return;
    SourceComboBox.ItemsSource = devices;
    SourceComboBox.SelectedItem = devices.FirstOrDefault(d => d.Id == selectedId) ?? devices.FirstOrDefault();
    if (devices.Count == 0)
      StatusText.Text = "Источники не настроены. Добавьте источник питания в параметрах конфигурации выбранного тестера.";
  }

  private void Source_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (SourceComboBox.SelectedItem is not IPowerSupply source) return;
    PowerSupplyDto dto = source.Convert();
    VoltageInput.Minimum = (double)source.VoltageManager.MinVoltage;
    VoltageInput.Maximum = (double)source.VoltageManager.MaxVoltage;
    VoltageInput.Increment = (double)source.VoltageManager.VoltageStep;
    CurrentInput.Minimum = (double)(source.CurrentManager.MinCurrent * 1000);
    CurrentInput.Maximum = (double)(source.CurrentManager.MaxCurrent * 1000);
    CurrentInput.Increment = (double)(source.CurrentManager.CurrentStep * 1000);
    OutputText.Text = "Состояние выхода: не прочитано";
    SetpointsText.Text = "Уставки прибора: не прочитаны";
    MeasurementsText.Text = "Напряжение: — В    Ток: — мА";
    ReadTimeText.Text = string.Empty;
    StatusText.Text = $"{source.Name} · тестер {source.NumberChassis} · {dto.ConnectionDetails}:{dto.Port} · {source.ConnectionInfo.GetConnectionStatus()}";
  }

  private async Task ExecuteAsync(string operation, Func<CancellationToken, Task> action)
  {
    if (_busy) return;
    bool acquired = false;
    try
    {
      EnsureRoot();
      if (!_runGuard.TryAcquire("Источник питания: " + operation, this, out string activeProcess))
        throw new InvalidOperationException($"Уже выполняется: {activeProcess}");
      acquired = true;
      _busy = true;
      OperationPanel.IsEnabled = false;
      CancelButton.IsEnabled = true;
      _operationCancellation = new CancellationTokenSource();
      await action(_operationCancellation.Token);
      if (_active && operation != "Загрузка источников") StatusText.Text = operation + ": выполнено.";
      LoggerUtility.LogInformation("Источник питания — " + operation + ": выполнено.", isDeviceLog: true);
    }
    catch (OperationCanceledException)
    {
      if (_active) StatusText.Text = operation + ": отменено.";
    }
    catch (Exception error)
    {
      if (_active) StatusText.Text = operation + ": " + error.Message;
      LoggerUtility.LogException(error, customMessage: "Источник питания — " + operation, isDeviceLog: true);
    }
    finally
    {
      _operationCancellation?.Dispose();
      _operationCancellation = null;
      _busy = false;
      OperationPanel.IsEnabled = RoleAuthorizationConfig.CurrentRole == RoleType.Root;
      CancelButton.IsEnabled = false;
      if (acquired) _runGuard.Release(this);
    }
  }

  private async Task ReadAsync(CancellationToken token)
  {
    var device = Device;
    if (!device.ConnectionInfo.IsConnected) throw new InvalidOperationException("Подключите источник питания.");
    decimal voltage = await device.VoltageManager.GetVoltageAsync(token);
    decimal current = await device.CurrentManager.GetCurrentAsync(token);
    bool output = await device.OutputManager.GetOutputStateAsync(token);
    decimal measuredVoltage = await device.MeasurementManager.MeasureVoltageAsync(token);
    decimal measuredCurrent = await device.MeasurementManager.MeasureCurrentAsync(token);
    token.ThrowIfCancellationRequested();
    if (!_active) return;
    SetpointsText.Text = $"Уставки прибора: {voltage:0.######} В / {current * 1000:0.###} мА";
    OutputText.Text = output ? "Выход включён" : "Выход выключен";
    MeasurementsText.Text = $"Напряжение: {measuredVoltage:0.######} В    Ток: {measuredCurrent * 1000:0.###} мА";
    ReadTimeText.Text = $"Прочитано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
  }

  private void RequireConnected()
  {
    if (!Device.ConnectionInfo.IsConnected) throw new InvalidOperationException("Подключите источник питания.");
  }

  private async void Connect_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Подключение", async token =>
  {
    using var context = Ask.Core.Services.UI.EquipmentExecutionContext.EnterExecution(token);
    var result = await Device.ConnectableManager.InitializeAsync();
    if (!result.Connect) throw new InvalidOperationException(result.Answer);
    await ReadAsync(token);
  });

  private async void Disconnect_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Отключение связи", async token =>
  {
    using var context = Ask.Core.Services.UI.EquipmentExecutionContext.EnterExecution(token);
    if (!await Device.ConnectableManager.DisconnectAsync()) throw new InvalidOperationException("Источник не подтвердил отключение.");
    OutputText.Text = "Связь отключена; состояние выхода не прочитано";
  });

  private async void SetVoltage_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Установка напряжения", async token =>
  {
    RequireConnected();
    await Device.VoltageManager.SetVoltageAsync((decimal)VoltageInput.Value, token);
    await ReadAsync(token);
  });

  private async void SetCurrent_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Установка ограничения тока", async token =>
  {
    RequireConnected();
    await Device.CurrentManager.SetCurrentAsync((decimal)CurrentInput.Value / 1000m, token);
    await ReadAsync(token);
  });

  private async void OutputOn_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Включение выхода", async token =>
  {
    RequireConnected();
    await Device.OutputManager.SetOutputStateAsync(true, token);
    await ReadAsync(token);
  });

  private async void OutputOff_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Выключение выхода", async token =>
  {
    await Device.OutputManager.SetOutputStateAsync(false, token);
    OutputText.Text = "Выход выключен";
    if (Device.ConnectionInfo.IsConnected) await ReadAsync(token);
  });

  private async void Read_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Чтение параметров", ReadAsync);
  private async void Reload_Click(object sender, RoutedEventArgs e) => await ExecuteAsync("Загрузка источников", token => LoadAsync(token: token));
  private void Cancel_Click(object sender, RoutedEventArgs e) => _operationCancellation?.Cancel();

}
