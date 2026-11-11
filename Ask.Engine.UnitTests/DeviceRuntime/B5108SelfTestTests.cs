using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Device;
using Ask.Engine.UnitTests.Services.Config;
using Moq;

namespace Ask.Engine.UnitTests.DeviceRuntime;

[Collection(nameof(ExecutionConfigCollection))]
public sealed class B5108SelfTestTests
{
  [Fact]
  public async Task VoltageCheck_MeasuresAllPointsAndRecordsOnlyOutOfToleranceResults()
  {
    var points = new List<MeasurementRange>();
    var run = new SelfTestRun(range =>
    {
      points.Add(range);
      return range.TargetValue switch
      {
        0.1 => range.LowerBound,
        0.2 => range.UpperBound,
        0.3 => range.UpperBound + 0.001,
        _ => range.TargetValue,
      };
    });

    await run.StartAsync();

    Assert.Equal(new[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
      points.Select(range => range.TargetValue));
    Assert.Equal(0.089, points[0].LowerBound);
    Assert.Equal(0.111, points[0].UpperBound);
    Assert.Equal(8.9, points[^1].LowerBound);
    Assert.Equal(9.1, points[^1].UpperBound);
    var test = Assert.Single(Assert.Single(run.Settings.DeviceResults).Tests);
    Assert.Contains("0,3 В", Assert.Single(test.Errors).Message);
    Assert.Equal(new[] { false, true, false }, run.OutputStates);
  }

  [Fact]
  public async Task VoltageCheck_WhenCancelled_DisablesOutputInMandatoryFinalization()
  {
    using var cancellation = new CancellationTokenSource();
    var run = new SelfTestRun(range =>
    {
      cancellation.Cancel();
      return range.TargetValue;
    });
    using var execution = EquipmentExecutionContext.EnterExecution(cancellation.Token);

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.StartAsync(cancellation.Token));

    Assert.Equal(new[] { false, true, false }, run.OutputStates);
    Assert.True(run.OutputDisabledDuringFinalization);
  }

  private sealed class SelfTestRun
  {
    private readonly B5108 _device;
    private readonly IMultimeter _meter;
    private readonly IUserInteractionService _messages = Mock.Of<IUserInteractionService>();

    public ActionSettings Settings { get; } = new() { StartDelegate = (_, _, _, _, _) => Task.CompletedTask };
    public List<bool> OutputStates { get; } = [];
    public bool OutputDisabledDuringFinalization { get; private set; }

    public SelfTestRun(Func<MeasurementRange, double> measure)
    {
      var connection = new Mock<IConnectable>();
      connection.Setup(x => x.InitializeAsync(It.IsAny<IUserInteractionService>())).ReturnsAsync((true, ""));
      var output = new Mock<IOutputManager>();
      output.Setup(x => x.SetOutputStateAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<IUserInteractionService>()))
        .Callback((bool enabled, CancellationToken token, IUserInteractionService? messages) =>
        {
          OutputStates.Add(enabled);
          if (!enabled && EquipmentExecutionContext.IsMandatoryFinalization)
          {
            Assert.False(token.CanBeCanceled);
            Assert.Null(messages);
            OutputDisabledDuringFinalization = true;
          }
        })
        .Returns(Task.CompletedTask);
      var current = new Mock<ICurrentManager>(MockBehavior.Strict);
      current.Setup(x => x.SetCurrentAsync(0.10m, It.IsAny<CancellationToken>(), It.IsAny<IUserInteractionService>()))
        .Returns(Task.CompletedTask);
      _device = new B5108
      {
        ConnectableManager = connection.Object,
        VoltageManager = Mock.Of<IVoltageManager>(),
        CurrentManager = current.Object,
        OutputManager = output.Object,
      };

      var voltage = new Mock<IDcVoltageMeasurement>();
      voltage.Setup(x => x.SetDCVoltageModeAsync(It.IsAny<IUserInteractionService>())).ReturnsAsync(true);
      voltage.Setup(x => x.MeasureDCVoltageAsync(It.IsAny<MeasurementRange>(), It.IsAny<IUserInteractionService>(), 0))
        .ReturnsAsync((MeasurementRange range, IUserInteractionService? _, double _) => measure(range));
      var meter = new Mock<IMultimeter>();
      meter.SetupGet(x => x.ConnectableManager).Returns(connection.Object);
      meter.SetupGet(x => x.DcVoltageManager).Returns(voltage.Object);
      _meter = meter.Object;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
      var manager = _device.SelfTestManager;
      var testType = (Enum)Enum.GetValues(manager.GetTestTypeEnum()).GetValue(0)!;
      return manager.StartSelfCheck(cancellationToken, testType, Settings, _messages, _device, _meter);
    }
  }
}
