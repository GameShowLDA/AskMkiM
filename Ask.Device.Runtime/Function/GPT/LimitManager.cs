using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Device.Runtime.Function.GPT
{
  public class LimitManager : ILimitManager
  {
    private IBreakdownTester BreakdownTester { get; init; }
    public LimitManager(IBreakdownTester breakdownTester)
    {
      BreakdownTester = breakdownTester;
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetHighLimitAsync(double value, IUserInteractionService? userMessageService = null)
    {
      switch (BreakdownTester.Mode)
      {
        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.ACW:
          return await BreakdownTester.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(value, userMessageService);

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.DCW:
          return await BreakdownTester.DcwManger.CurrentLimits.SetHighCurrentLimitAsync(value, userMessageService);

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR:
          return await BreakdownTester.IrManger.ResistanceLimits.SetHighResistanceLimitAsync(value, userMessageService);

        default: return (false, string.Empty);
      }
    }

    /// <inheritdoc />
    public async Task<double> GetHighLimitAsync()
    {
      switch (BreakdownTester.Mode)
      {
        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.ACW:
          return await BreakdownTester.AcwManger.CurrentLimits.GetHighCurrentLimitAsync();

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.DCW:
          return await BreakdownTester.DcwManger.CurrentLimits.GetHighCurrentLimitAsync();

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR:
          return await BreakdownTester.IrManger.ResistanceLimits.GetLowResistanceLimitAsync();

        default: return -1;
      }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string Message)> SetLowLimitAsync(double value, IUserInteractionService? userMessageService = null)
    {
      switch (BreakdownTester.Mode)
      {
        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.ACW:
          return await BreakdownTester.AcwManger.CurrentLimits.SetLowCurrentLimitAsync(value, userMessageService);

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.DCW:
          return await BreakdownTester.DcwManger.CurrentLimits.SetLowCurrentLimitAsync(value, userMessageService);

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR:
          return await BreakdownTester.IrManger.ResistanceLimits.SetLowResistanceLimitAsync(value, userMessageService);

        default: return (false, string.Empty);
      }
    }

    /// <inheritdoc />
    public async Task<double> GetLowLimitAsync()
    {
      switch (BreakdownTester.Mode)
      {
        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.ACW:
          return await BreakdownTester.AcwManger.CurrentLimits.GetLowCurrentLimitAsync();

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.DCW:
          return await BreakdownTester.DcwManger.CurrentLimits.GetLowCurrentLimitAsync();

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR:
          return await BreakdownTester.IrManger.ResistanceLimits.GetLowResistanceLimitAsync();

        default: return -1;
      }
    }
  }
}
