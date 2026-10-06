using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Device.Runtime.Device;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.UnitTests.TestInfrastructure;
using Moq;

namespace Ask.Engine.UnitTests.ControlCommandExecutor;

public sealed class BreakdownInputValidationTests
{
  [Theory]
  [InlineData(MeasurementTypeCommand.SI, true)]
  [InlineData(MeasurementTypeCommand.PI_ACW, true)]
  [InlineData(MeasurementTypeCommand.PI_DCW, true)]
  [InlineData(MeasurementTypeCommand.PI_ACW, false)]
  [InlineData(MeasurementTypeCommand.PI_DCW, false)]
  public void InvalidVoltageIsRejectedWithoutEquipmentAccess(MeasurementTypeCommand mode, bool voltageField)
  {
    using var device = new GPT79904();
    device.DeviceProtocol = new Mock<Ask.Core.Shared.Interfaces.DeviceInterfaces.IDeviceProtocol>(MockBehavior.Strict).Object;
    var data = new DataModel { Param = voltageField ? 100 : 1, Voltage = voltageField ? 1 : 100 };
    Assert.Throws<InputValidationException>(() =>
      BreakdownInputValidator.ValidateMeasurement(device, mode, data, voltageField));
  }

  [Theory]
  [InlineData(125, true)]
  [InlineData(126, false)]
  [InlineData(1000, true)]
  [InlineData(1001, false)]
  public void NestedSiUsesIrRange(double voltage, bool valid)
  {
    using var device = new GPT79904();
    var command = new PiCommandModel
    {
      Voltage = 100, VoltageType = VoltageEnum.Type.ACW,
      SiCommand = new SiCommandModel { Voltage = voltage }
    };
    var exception = Record.Exception(() => BreakdownInputValidator.ValidateCommand(device, command));
    if (valid)
      Assert.Null(exception);
    else
      Assert.IsType<InputValidationException>(exception);
  }

  [Fact]
  public void PreflightUsesCurrentConfiguration()
  {
    using var device = new GPT79904();
    device.AcwManger.VoltageRange.MaxVoltage = 650;
    Assert.Throws<InputValidationException>(() => BreakdownInputValidator.ValidateCommand(device,
      new PiCommandModel { Voltage = 652, VoltageType = VoltageEnum.Type.ACW }));
  }

  [Fact]
  public Task ProgramWithLaterInputErrorDoesNotExecuteFirstCommand() => WpfTestHost.RunAsync(async () =>
  {
    var console = new Mock<IUserInteractionService>();
    var editor = new Mock<ITextEditorAdapter>();
    var invalid = new SiCommandModel
    {
      Errors = [new ErrorItem { Code = ErrorCode.Metrology_Validation_InvalidVoltage, Description = "Неверное напряжение" }]
    };
    var manager = new CommandExecutionManager(console.Object, editor.Object,
      [new RmCommandModel(), invalid], null);
    await Assert.ThrowsAsync<InputValidationException>(() => manager.ExecuteAllAsync());
    editor.Verify(x => x.SetActiveLine(It.IsAny<int>()), Times.Never);
    console.Verify(x => x.CompleteCommandAsync(It.IsAny<bool>()), Times.Never);
  });
}
