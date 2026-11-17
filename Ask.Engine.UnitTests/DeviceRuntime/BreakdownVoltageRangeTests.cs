using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Runtime.Device;
using Moq;

namespace Ask.Engine.UnitTests.DeviceRuntime;

public sealed class BreakdownVoltageRangeTests
{
  [Fact]
  public void CompositionAndConfigurationResetPreserveModeRanges()
  {
    using var device = new GPT79904();
    var acw = device.AcwManger.VoltageRange;
    var dcw = device.DcwManger.VoltageRange;
    var ir = device.IrManger.VoltageRange;
    acw.MaxVoltage = 650;
    dcw.MaxVoltage = 900;
    ir.Exceptions.Add(175);

    Ask.Device.Application.Composition.DeviceApplicationComposer.Compose(device);
    Ask.Device.Application.Composition.DeviceApplicationComposer.Compose(device);
    device.AcwManger.Config.ResetConfiguration();
    device.DcwManger.Config.ResetConfiguration();
    device.IrManger.Config.ResetConfiguration();
    device.Mode = Ask.Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.ACW;
    device.Mode = Ask.Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR;

    Assert.Same(acw, device.AcwManger.VoltageRange);
    Assert.Same(dcw, device.DcwManger.VoltageRange);
    Assert.Same(ir, device.IrManger.VoltageRange);
    Assert.Equal(650, device.Convert().AcwVoltageRange.MaxVoltage);
    Assert.Equal(900, device.Convert().DcwVoltageRange.MaxVoltage);
    Assert.Contains(175, device.Convert().IrVoltageRange.Exceptions);
  }

  [Fact]
  public async Task AdapterUsesRangeAppliedFromDtoForVoltageValidation()
  {
    using var device = new GPT79904();
    Ask.Device.Application.Composition.DeviceApplicationComposer.Compose(device);
    var dto = device.Convert();
    dto.AcwVoltageRange.MaxVoltage = 650;
    Ask.DataBase.Engine.Mapping.Device.BreakdownTesterMapper.ApplyDto(device, dto);
    var protocol = new Mock<IDeviceProtocol>(MockBehavior.Strict);
    device.DeviceProtocol = protocol.Object;

    await Assert.ThrowsAsync<Ask.Core.Services.Errors.Device.DeviceException>(
      () => device.AcwManger.Voltage.SetVoltageAsync(652));
    protocol.VerifyNoOtherCalls();
    Assert.Equal(650, device.AcwManger.VoltageRange.MaxVoltage);
    Assert.NotSame(dto.AcwVoltageRange, device.AcwManger.VoltageRange);
  }

  [Theory]
  [InlineData(50, true)]
  [InlineData(100, true)]
  [InlineData(125, true)]
  [InlineData(150, true)]
  [InlineData(1000, true)]
  [InlineData(49, false)]
  [InlineData(126, false)]
  [InlineData(1001, false)]
  [InlineData(double.NaN, false)]
  [InlineData(double.PositiveInfinity, false)]
  public void IrChecksBoundsStepAndException(double value, bool expected)
  {
    using var device = new GPT79904();
    Assert.Equal(expected, device.IrManger.VoltageRange.IsAllowed(value));
  }

  [Theory]
  [InlineData(50, true)]
  [InlineData(52, true)]
  [InlineData(700, true)]
  [InlineData(51, false)]
  [InlineData(125, false)]
  [InlineData(702, false)]
  public void AcwUsesSystemMaximumAndTwoVoltStep(double value, bool expected)
  {
    using var device = new GPT79904();
    Assert.Equal(expected, device.AcwManger.VoltageRange.IsAllowed(value));
    Assert.Equal(1000, device.DcwManger.VoltageRange.MaxVoltage);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-2)]
  [InlineData(double.NaN)]
  public void InvalidStepIsRejected(double step)
  {
    var range = new Ask.Core.Shared.DTO.Devices.Breakdown.VoltageRange
      { MinVoltage = 50, MaxVoltage = 1000, Step = step, Exceptions = [125] };
    Assert.False(range.IsAllowed(125));
  }

  [Fact]
  public async Task InvalidVoltageDoesNotReachTransportEvenIfItMatchesCachedValue()
  {
    using var device = new GPT79904();
    var protocol = new Mock<IDeviceProtocol>(MockBehavior.Strict);
    device.DeviceProtocol = protocol.Object;
    Assert.False((await device.IrManger.Voltage.SetVoltageAsync(0)).Success);
    Assert.False((await device.AcwManger.Voltage.SetVoltageAsync(51)).Success);
    protocol.VerifyNoOtherCalls();
  }
}
