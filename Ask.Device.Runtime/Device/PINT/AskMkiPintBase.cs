using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

namespace Ask.Device.Runtime.Device.PINT;

public abstract class AskMkiPintBase : AskMkiDeviceBase, IAskMkiPint
{
  private readonly double _voltageStep;
  private readonly double _currentStep;
  private readonly bool _useBcdCode;

  protected AskMkiPintBase(string name, string description, DeviceType deviceType, int pintNumber, double voltageStep, double currentStep, bool useBcdCode)
    : base(name, description, deviceType)
  {
    PintNumber = pintNumber;
    _voltageStep = voltageStep;
    _currentStep = currentStep;
    _useBcdCode = useBcdCode;
  }

  public int PintNumber { get; }

  public async Task SetOutputAsync(IAskMkiController controller, double volts, double amps, ushort positiveBus, ushort negativeBus, CancellationToken cancellationToken = default)
  {
    await SetBusesAsync(controller, positiveBus, negativeBus, cancellationToken).ConfigureAwait(false);
    await controller.WriteSubRegisterAsync(GetRegister(), LegacyAskPintSubRegister.Voltage, ToVoltageWord(volts), cancellationToken).ConfigureAwait(false);
    await controller.WriteSubRegisterAsync(GetRegister(), LegacyAskPintSubRegister.Current, ToCurrentWord(amps), cancellationToken).ConfigureAwait(false);
  }

  public async Task SetBusesAsync(IAskMkiController controller, ushort positiveBus, ushort negativeBus, CancellationToken cancellationToken = default)
  {
    await controller.WriteSubRegisterAsync(GetRegister(), LegacyAskPintSubRegister.PositiveBus, ToBusWord(positiveBus), cancellationToken).ConfigureAwait(false);
    await controller.WriteSubRegisterAsync(GetRegister(), LegacyAskPintSubRegister.NegativeBus, ToBusWord(negativeBus), cancellationToken).ConfigureAwait(false);
  }

  public async Task ResetAsync(IAskMkiController controller, CancellationToken cancellationToken = default)
  {
    await controller.WriteSubRegisterAsync(GetRegister(), LegacyAskPintSubRegister.Voltage, ToVoltageWord(_voltageStep * 2.0), cancellationToken).ConfigureAwait(false);
    await controller.WriteSubRegisterAsync(GetRegister(), LegacyAskPintSubRegister.Current, ToCurrentWord(_currentStep * 2.0), cancellationToken).ConfigureAwait(false);
    await SetBusesAsync(controller, 0, 0, cancellationToken).ConfigureAwait(false);
  }

  private LegacyAskRegister GetRegister()
  {
    return PintNumber == 3 ? LegacyAskRegister.Gui3 : LegacyAskRegister.Gui4;
  }

  private ushort ToVoltageWord(double volts)
  {
    return ToCode(Math.Max(0.0, volts), _voltageStep);
  }

  private ushort ToCurrentWord(double amps)
  {
    return ToCode(Math.Max(0.0, amps), _currentStep);
  }

  private ushort ToCode(double value, double step)
  {
    double safeStep = step > 0 ? step : 1.0;
    int code = (int)Math.Round(value / safeStep);
    if (code <= 0 && value > 0)
    {
      code = 1;
    }

    code = Math.Clamp(code, 0, 0x0FFF);
    return _useBcdCode ? ToBcdWord(code) : (ushort)code;
  }

  private static ushort ToBcdWord(int value)
  {
    int safeValue = Math.Clamp(value, 0, 999);
    return (ushort)(((safeValue / 100) << 8) | (((safeValue / 10) % 10) << 4) | (safeValue % 10));
  }

  private static ushort ToBusWord(ushort bus)
  {
    return (ushort)(bus & 0x00FF);
  }
}
