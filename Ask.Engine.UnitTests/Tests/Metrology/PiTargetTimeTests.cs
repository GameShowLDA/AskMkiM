using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.Tests.Metrology;
using Ask.Engine.Tests.Metrology.MeasurementSystem;
using Moq;

namespace Ask.Engine.UnitTests.Tests.Metrology;

public sealed class PiTargetTimeTests
{
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task ConfigureMeterRecordsSelectedTimeBeforeSendingItToDevice(bool acw)
  {
    var device = new Mock<IBreakdownTester> { DefaultValue = DefaultValue.Mock };
    var time = new Ask.Device.Runtime.Function.GPT.TimeManager(device.Object);
    device.SetupGet(x => x.Time).Returns(time);
    var deviceTime = acw ? device.Object.AcwManger.Time : device.Object.DcwManger.Time;
    var sentTimes = new List<double>();
    Mock.Get(deviceTime)
      .Setup(x => x.SetTestTimeAsync(It.IsAny<double>(), It.IsAny<IUserInteractionService>()))
      .Callback<double, IUserInteractionService>((value, _) =>
      {
        Assert.Equal(value, time.GetTargetTime());
        sentTimes.Add(value);
      })
      .ReturnsAsync((true, string.Empty));
    var role = acw ? MeasurementTypeCommand.PI_ACW : MeasurementTypeCommand.PI_DCW;
    BaseMeasurement measurement = acw
      ? new ModePiAcw.PiMeasurement(Mock.Of<IReferenceVoltageRequestService>())
      : new ModePiDcw.PiMeasurement(Mock.Of<IReferenceVoltageRequestService>());
    measurement.Devices[role] = [device.Object];
    var messages = new Mock<IUserInteractionService> { DefaultValue = DefaultValue.Mock };

    var random = new Random(1020);
    var selectedTimes = new[] { 1d, 5d, 60d, 0.1d, 1.5d }
      .Concat(Enumerable.Range(0, 95).Select(_ => Math.Round(0.1 + random.NextDouble() * 59.9, 1)))
      .ToArray();
    foreach (double selectedTime in selectedTimes)
    {
      await measurement.ConfigureMeter(messages.Object, role,
        new DataModel { Time = selectedTime, RampTime = 1, Param = 124 });
      Assert.Equal(selectedTime, time.GetTargetTime());
    }

    Assert.Equal(selectedTimes, sentTimes);
  }
}
