using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Device.Runtime.Function.Base.Connected;
using Moq;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Ask.Device.Runtime.Device;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;

namespace Ask.Engine.UnitTests.DeviceRuntime;

public sealed class MultimeterInitializationTests
{
  [Fact]
  public async Task TcpInitialization_FinalNoResponseIncludesThirdAttempt()
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var device = new KeysightDevice { IPAddress = IPAddress.Loopback };
    device.ConnectedProfile.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var error = new DeviceNoResponseException(device, "*IDN?", 1000);
      var protocol = new Mock<IDeviceProtocol>();
      device.DeviceProtocol = protocol.Object;
      protocol.Setup(x => x.QueryAsync("*IDN?", 0, 1000, device.ConnectedProfile.Port,
        0, It.IsAny<CancellationToken>())).ThrowsAsync(error);
      Assert.False((await device.ConnectableManager.InitializeAsync()).Connect);
      protocol.Verify(x => x.QueryAsync("*IDN?", 0, 1000, device.ConnectedProfile.Port,
        0, It.IsAny<CancellationToken>()), Times.Exactly(3));
      Assert.Equal("Инициализация (Попытка 3/3)", error.ToMessage().Message);
      Assert.Equal("[СБОЙ ОБМЕНА]", error.ToMessage().GetQualityPrefix());
    }
    finally
    {
      device.ConnectedProfile.Stream?.Dispose();
      device.ConnectedProfile.TcpClient?.Dispose();
      ExecutionConfig.SetIdleMode(idle);
    }
  }

  [Theory]
  [InlineData(true, 1)]
  [InlineData(true, 2)]
  [InlineData(true, 3)]
  [InlineData(false, 1)]
  [InlineData(false, 2)]
  [InlineData(false, 3)]
  public async Task InitializationMessage_AppendsAttemptOnlyAfterFirst(bool success, int attempt)
  {
    bool visible = DeviceDisplayConfig.GetExecutionParametersVisibility();
    try
    {
      DeviceDisplayConfig.SetExecutionParametersVisibility(true);
      var device = new KeysightDevice();
      var output = new Mock<IUserInteractionService>();
      await MultimeterResponseProcessor.PublishInitializationResultAsync(device, success, "Нет ответа", output.Object, attempt);
      var invocation = Assert.Single(output.Invocations, x => x.Method.Name == "ShowMessageAsync");
      var message = Assert.IsType<ShowMessageModel>(invocation.Arguments[0]);
      string expected = success ? "Инициализация" : "Инициализация: Нет ответа";
      if (attempt > 1)
        expected += $" (Попытка {attempt}/3)";
      Assert.Equal(expected, message.Message);
      Assert.Equal(success ? "[ОК]" : "[ERR]", message.GetQualityPrefix());
    }
    finally { DeviceDisplayConfig.SetExecutionParametersVisibility(visible); }
  }

  [Fact]
  public async Task TcpInitialization_UsesRetryPipelineAndDisablesSoundAfterSuccess()
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    bool visible = DeviceDisplayConfig.GetExecutionParametersVisibility();
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var device = new KeysightDevice { IPAddress = IPAddress.Loopback };
    device.ConnectedProfile.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
    device.ConnectedProfile.Timeout = 9999;
    try
    {
      ExecutionConfig.SetIdleMode(false);
      DeviceDisplayConfig.SetExecutionParametersVisibility(true);
      var protocol = new Mock<IDeviceProtocol>();
      device.DeviceProtocol = protocol.Object;
      int identificationAttempts = 0;
      protocol.Setup(x => x.QueryAsync(It.IsAny<string>(), 0, It.IsAny<int>(),
          It.IsAny<int>(), 0, It.IsAny<CancellationToken>()))
        .Returns((string command, double _, int timeout, int port, int _, CancellationToken _) =>
        {
          if (command == "*IDN?")
          {
            Assert.Equal(1000, timeout);
            Assert.Equal(device.ConnectedProfile.Port, port);
            identificationAttempts++;
            return identificationAttempts < 3
              ? Task.FromException<string>(new DeviceNoResponseException(device, command, timeout))
              : Task.FromResult("Keysight Technologies,34465A,TEST,1.0");
          }
          Assert.Equal(3, identificationAttempts);
          Assert.Equal("SYST:BEEP:STAT OFF", command);
          return Task.FromResult(string.Empty);
        });
      var output = new Mock<IUserInteractionService>();
      var result = await device.ConnectableManager.InitializeAsync(output.Object);
      Assert.True(result.Connect);
      Assert.Equal(3, identificationAttempts);
      var invocation = Assert.Single(output.Invocations, x => x.Method.Name == "ShowMessageAsync");
      Assert.Equal("Инициализация (Попытка 3/3)", Assert.IsType<ShowMessageModel>(invocation.Arguments[0]).Message);
      protocol.Verify(x => x.QueryAsync("SYST:BEEP:STAT OFF", 0, 0, 0, 0, It.IsAny<CancellationToken>()), Times.Once);
    }
    finally
    {
      device.ConnectedProfile.Stream?.Dispose();
      device.ConnectedProfile.TcpClient?.Dispose();
      ExecutionConfig.SetIdleMode(idle);
      DeviceDisplayConfig.SetExecutionParametersVisibility(visible);
    }
  }

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  public async Task Initialize_StopsOnSuccessAndUsesOneSecondTimeoutWithIncreasingPauses(int successAttempt)
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var device = new Mock<IMultimeter>();
      var protocol = new Mock<IDeviceProtocol>();
      device.SetupGet(x => x.Name).Returns("Keysight 34465A");
      device.SetupGet(x => x.DeviceProtocol).Returns(protocol.Object);
      var sends = new List<TimeSpan>();
      var clock = Stopwatch.StartNew();
      protocol.Setup(x => x.QueryAsync("*IDN?", 0, 1000, 5025, 0, It.IsAny<CancellationToken>()))
        .Returns(() =>
        {
          sends.Add(clock.Elapsed);
          return sends.Count == successAttempt
            ? Task.FromResult(" Keysight Technologies,34465A,TEST,1.0 ")
            : Task.FromException<string>(new DeviceNoResponseException(device.Object, "*IDN?", 1000));
        });

      var result = await MultimeterInitialization.InitializeAsync(device.Object, "*IDN?", 5025);

      Assert.True(result.Connect);
      Assert.Equal("Keysight Technologies,34465A,TEST,1.0", result.Answer);
      Assert.Equal(successAttempt, sends.Count);
      for (int index = 1; index < sends.Count; index++)
        Assert.True(sends[index] - sends[index - 1] >= TimeSpan.FromSeconds(index));
      protocol.Verify(x => x.QueryAsync("*IDN?", 0, 1000, 5025, 0, It.IsAny<CancellationToken>()), Times.Exactly(successAttempt));
      protocol.VerifyNoOtherCalls();
    }
    finally { ExecutionConfig.SetIdleMode(idle); }
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task Initialize_StopsAfterThreeFailedAttempts(bool throws)
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      var device = new Mock<IMultimeter>();
      var protocol = new Mock<IDeviceProtocol>();
      device.SetupGet(x => x.Name).Returns("Keysight 34465A");
      device.SetupGet(x => x.DeviceProtocol).Returns(protocol.Object);
      var error = new DeviceNoResponseException(device.Object, "*IDN?", 1000);
      protocol.Setup(x => x.QueryAsync("*IDN?", 0, 1000, 0, 0, It.IsAny<CancellationToken>()))
        .Returns(() => throws ? Task.FromException<string>(error) : Task.FromResult(string.Empty));
      if (throws)
        Assert.Same(error, await Assert.ThrowsAsync<DeviceNoResponseException>(() => MultimeterInitialization.InitializeAsync(device.Object, "*IDN?")));
      else
        Assert.False((await MultimeterInitialization.InitializeAsync(device.Object, "*IDN?")).Connect);
      protocol.Verify(x => x.QueryAsync("*IDN?", 0, 1000, 0, 0, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
    finally { ExecutionConfig.SetIdleMode(idle); }
  }

  [Fact]
  public async Task Initialize_CancellationDuringPausePreventsNextAttempt()
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      using var cancellation = new CancellationTokenSource();
      using var context = EquipmentExecutionContext.EnterExecution(cancellation.Token);
      var device = new Mock<IMultimeter>();
      var protocol = new Mock<IDeviceProtocol>();
      device.SetupGet(x => x.DeviceProtocol).Returns(protocol.Object);
      protocol.Setup(x => x.QueryAsync("*IDN?", 0, 1000, 0, 0, It.IsAny<CancellationToken>()))
        .Returns(() =>
        {
          cancellation.CancelAfter(100);
          return Task.FromException<string>(new DeviceNoResponseException(device.Object, "*IDN?", 1000));
        });
      await Assert.ThrowsAnyAsync<OperationCanceledException>(() => MultimeterInitialization.InitializeAsync(device.Object, "*IDN?"));
      protocol.Verify(x => x.QueryAsync("*IDN?", 0, 1000, 0, 0, It.IsAny<CancellationToken>()), Times.Once);
    }
    finally { ExecutionConfig.SetIdleMode(idle); }
  }

  [Fact]
  public async Task Initialize_IdleUsesEmulatorWithoutRealQueries()
  {
    bool idle = ExecutionConfig.GetIsIdleModeEnabled();
    try
    {
      ExecutionConfig.SetIdleMode(true);
      var device = new Mock<IMultimeter>();
      device.SetupGet(x => x.Name).Returns("Keysight 34465A");
      var protocol = new Mock<IDeviceProtocol>(MockBehavior.Strict);
      device.SetupGet(x => x.DeviceProtocol).Returns(protocol.Object);
      Assert.True((await MultimeterInitialization.InitializeAsync(device.Object, "*IDN?")).Connect);
      protocol.VerifyNoOtherCalls();
    }
    finally { ExecutionConfig.SetIdleMode(idle); }
  }
}
