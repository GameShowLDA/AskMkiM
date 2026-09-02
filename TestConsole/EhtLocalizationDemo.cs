using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Parser.Eht;
using Ask.Engine.ControlCommandAnalyser.Parser.Rm;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;
using Ask.Engine.ControlCommandExecutor.Execution;
using Moq;

namespace TestConsole;

/// <summary>
/// Выполняет команду ЭТ через штатный parser → executor → localization pipeline.
/// </summary>
internal static class EhtLocalizationDemo
{
  private const string CommandLine1 =
    "160 ЭТ Ом<40 *К1/11-15,К1/61-65#К1/21-25*К1/31-35*К1/41-45";
  private const string CommandLine2 = "*К1/71-75,К1/81-85*К1/91-95*";
  private static readonly Lazy<Dispatcher> WpfDispatcher = new(CreateWpfDispatcher);

  public static Task RunAsync() => WpfDispatcher.Value.InvokeAsync(RunCoreAsync).Task.Unwrap();

  private static async Task RunCoreAsync()
  {
    Console.OutputEncoding = Encoding.UTF8;
    var previousSettings = ExecutionConfig.GetExecutionModelSnapshot();
    var previousCommands = CommandsModel.CommandModels.ToList();

    try
    {
      ConfigureIdleExecution();
      EhtCommandModel command = ParseCommand();
      var points = command.Scheme.EnumeratePoints().ToList();
      var messages = new List<ShowMessageModel>();
      var errors = new List<ErrorItem>();

      using var equipment = new EhtEquipmentScope(points);
      var interaction = CreateInteractionService(messages);
      var editor = new Mock<ITextEditorAdapter>();
      var manager = new CommandExecutionManager(
        interaction.Object, editor.Object, [command], "TestConsole/eht-localization.opk");
      manager.AddError += errors.Add;

      Console.WriteLine();
      Console.WriteLine("Полный путь: EhtCommandParser → CommandExecutionManager → EhtCommandExecutor");
      Console.WriteLine("             → PairwiseFirstPointCheckerAlt → EhtHighResistanceLocalizationService");
      Console.WriteLine("Команда:");
      Console.WriteLine(CommandLine1);
      Console.WriteLine(CommandLine2);
      Console.WriteLine();

      await manager.ExecuteAllAsync();

      foreach (var message in messages)
        Console.WriteLine($"[{message.Status}] {message}");

      Console.WriteLine();
      Console.WriteLine("Зарегистрированные ошибки:");
      foreach (var error in errors)
        Console.WriteLine($"{error.Code?.GetTag()} | {error.Command} | {error.MeasureResult} | {error.Description}");
    }
    finally
    {
      CommandsModel.CommandModels = previousCommands;
      await ExecutionConfig.SetExecutionModel(previousSettings);
    }
  }

  private static EhtCommandModel ParseCommand()
  {
    var rmParser = new RmCommandParser(() => [new LegacyRelaySwitchModuleInfo(1, 100)]);
    var rm = (RmCommandModel)rmParser.Parse("30", "РМ", 1, ["30 РМ К1/1-100=1.1.1-100"]);
    CommandsModel.CommandModels = [rm];
    var command = (EhtCommandModel)new EhtCommandParser().Parse(
      "160", "ЭТ", 2, [CommandLine1, CommandLine2]);

    if (command.Errors.Count > 0)
      throw new InvalidOperationException(
        "Команда ЭТ не прошла разбор: " + string.Join("; ", command.Errors.Select(error => error.Description)));

    command.FormattedStartLineNumber = 2;
    return command;
  }

  private static void ConfigureIdleExecution()
  {
    ExecutionConfig.SetIdleMode(true);
    ExecutionConfig.SetIsErrorSimulationMode(true);
    ExecutionConfig.SetStepByStepMode(false);
    ExecutionConfig.SetStopOnError(false);
    DeviceDisplayConfig.SetExecutionParametersVisibility(false);
    DeviceDisplayConfig.SetMeasurementResultsVisibility(true);
    DeviceDisplayConfig.SetMachineAddressVisibility(false);
  }

  private static Mock<IUserInteractionService> CreateInteractionService(List<ShowMessageModel> messages)
  {
    var service = new Mock<IUserInteractionService>();
    service.SetupProperty(item => item.Header, string.Empty);
    service.Setup(item => item.GetCancellationToken()).Returns(CancellationToken.None);
    service.Setup(item => item.GetLastLineNumber()).Returns(() => messages.Count);
    service.Setup(item => item.GetText()).Returns(string.Empty);
    service.SetupGet(item => item.ButtonService).Returns(Mock.Of<IButtonService>());
    service.Setup(item => item.WaitUserActionAsync(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
      .ReturnsAsync(UserAction.None);
    service.Setup(item => item.ConfirmControlProgramCommandRetryAsync(It.IsAny<int>()))
      .ReturnsAsync(UserAction.None);
    service.Setup(item => item.ShowMessageAsync(
        It.IsAny<ShowMessageModel>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(),
        It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
      .Callback<ShowMessageModel, bool, bool, bool, bool, string, string, int>(
        (message, _, _, _, _, _, _, _) => messages.Add(message))
      .Returns(Task.CompletedTask);
    service.Setup(item => item.AppendEmptyLineAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
    service.Setup(item => item.MoveToLineAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
    service.Setup(item => item.CompleteCommandAsync(It.IsAny<bool>())).Returns(Task.CompletedTask);
    return service;
  }

  private static Dispatcher CreateWpfDispatcher()
  {
    var ready = new TaskCompletionSource<Dispatcher>(TaskCreationOptions.RunContinuationsAsynchronously);
    var thread = new Thread(() =>
    {
      var application = Application.Current ?? new Application();
      application.Resources["TestsProtocolMessageSuccesForeground"] = new SolidColorBrush(Colors.Green);
      application.Resources["TestsProtocolMessageErrorForeground"] = new SolidColorBrush(Colors.Red);
      application.Resources["TestsProtocolHeaderForeground"] = new SolidColorBrush(Colors.White);
      application.Resources["TestsProtocolMessageForeground"] = new SolidColorBrush(Colors.White);
      application.Resources["TestsProtocolTimeForeground"] = new SolidColorBrush(Colors.White);
      application.Resources["YellowColorSolidColorBrush"] = new SolidColorBrush(Colors.Yellow);
      application.Resources["LightBlueColorSolidColorBrush"] = new SolidColorBrush(Colors.LightBlue);
      ready.SetResult(Dispatcher.CurrentDispatcher);
      Dispatcher.Run();
    }) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    return ready.Task.GetAwaiter().GetResult();
  }

  private sealed class EhtEquipmentScope : IDisposable
  {
    private readonly HashSet<int> pointsOnBusA = [];
    private readonly HashSet<int> pointsOnBusB = [];

    public EhtEquipmentScope(List<PointModel> points)
    {
      var pointManager = new Mock<IPointManager>();
      pointManager.Setup(x => x.ConnectRelayAsync(It.IsAny<BusPoint>(), It.IsAny<int>(), It.IsAny<IUserInteractionService?>()))
        .Callback<BusPoint, int, IUserInteractionService?>((bus, point, _) => Connect(bus, point))
        .ReturnsAsync(true);
      pointManager.Setup(x => x.DisconnectRelayAsync(It.IsAny<BusPoint>(), It.IsAny<int>(), It.IsAny<IUserInteractionService?>()))
        .Callback<BusPoint, int, IUserInteractionService?>((bus, point, _) => Disconnect(bus, point))
        .ReturnsAsync(true);
      pointManager.Setup(x => x.DisconnectingAllPoint(It.IsAny<IUserInteractionService?>()))
        .Callback(ClearConnections).ReturnsAsync(true);

      var busManager = new Mock<IBusManager>();
      busManager.Setup(x => x.ConnectBusAsync(It.IsAny<SwitchingBus>(), It.IsAny<IUserInteractionService?>()))
        .ReturnsAsync(true);
      var relay = new Mock<IRelaySwitchModule>();
      relay.SetupGet(x => x.NumberChassis).Returns(1);
      relay.SetupGet(x => x.Number).Returns(1);
      relay.SetupGet(x => x.BusType).Returns(SwitchingBusNew.AB1);
      relay.SetupGet(x => x.PointManager).Returns(pointManager.Object);
      relay.SetupGet(x => x.BusManager).Returns(busManager.Object);

      var continuity = new Mock<IContinuityMeasurement>();
      continuity.Setup(x => x.SetContinuityModeAsync(It.IsAny<IUserInteractionService?>())).ReturnsAsync(true);
      continuity.Setup(x => x.CheckContinuityAsync(
          It.IsAny<MeasurementRange>(), It.IsAny<IUserInteractionService?>(), It.IsAny<double>()))
        .ReturnsAsync(MeasureCurrentConnection);
      var meter = new Mock<IMultimeter>();
      meter.SetupGet(x => x.ContinuityManager).Returns(continuity.Object);

      var connector = new Mock<IConnectorDeviceBusCommutation>();
      connector.Setup(x => x.ConnectMultimeter(It.IsAny<SwitchingBusNew>(), It.IsAny<IUserInteractionService?>()))
        .ReturnsAsync(true);
      var switchingDevice = new Mock<ISwitchingDevice>();
      switchingDevice.SetupGet(x => x.ConnectorManager).Returns(connector.Object);

      SetEquipment("AnalyzedPoints", points);
      SetEquipment("PointsMap", points.ToDictionary(point => point.Mnemonic, point => point.ToString()));
      SetEquipment("ValidRelayModules", new List<IRelaySwitchModule> { relay.Object });
      SetEquipment("ValidSwitchingDevice", switchingDevice.Object);
      SetEquipment("ValidFastMeter", meter.Object);
      SetEquipment("ValidBreakdownTester", null);
    }

    public void Dispose()
    {
      SetEquipment("AnalyzedPoints", null);
      SetEquipment("PointsMap", new Dictionary<string, string>());
      SetEquipment("ValidRelayModules", null);
      SetEquipment("ValidSwitchingDevice", null);
      SetEquipment("ValidFastMeter", null);
      SetEquipment("ValidBreakdownTester", null);
    }

    private double MeasureCurrentConnection()
    {
      int? pointA = pointsOnBusA.Count == 1 ? pointsOnBusA.Single() : null;
      if (!pointA.HasValue || pointsOnBusB.Contains(pointA.Value)) return 1;
      int? pointB = pointsOnBusB.Count == 1 ? pointsOnBusB.Single() : null;
      if (!pointB.HasValue) return 1;
      bool separated = IsInRange(pointA.Value, 71, 75) && IsInRange(pointB.Value, 81, 85)
        || IsInRange(pointB.Value, 71, 75) && IsInRange(pointA.Value, 81, 85);
      return separated ? 50 : 2;
    }

    private void Connect(BusPoint bus, int point)
    {
      if (bus is BusPoint.A or BusPoint.AB) pointsOnBusA.Add(point);
      if (bus is BusPoint.B or BusPoint.AB) pointsOnBusB.Add(point);
    }

    private void Disconnect(BusPoint bus, int point)
    {
      if (bus is BusPoint.A or BusPoint.AB) pointsOnBusA.Remove(point);
      if (bus is BusPoint.B or BusPoint.AB) pointsOnBusB.Remove(point);
    }

    private void ClearConnections()
    {
      pointsOnBusA.Clear();
      pointsOnBusB.Clear();
    }

    private static bool IsInRange(int point, int first, int last) => point >= first && point <= last;

    private static void SetEquipment(string propertyName, object? value)
    {
      var property = typeof(EquipmentService).GetProperty(
        propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      property?.GetSetMethod(nonPublic: true)?.Invoke(null, [value]);
    }
  }
}
