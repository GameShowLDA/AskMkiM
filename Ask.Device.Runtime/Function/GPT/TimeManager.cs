using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Device.Runtime.Function.GPT
{
  public class TimeManager : ITimeManager
  {
    private IBreakdownTester BreakdownTester { get; init; }
    public TimeManager(IBreakdownTester breakdownTester)
    {
      BreakdownTester = breakdownTester;
    }

    public async Task<double> GetRampTimeAsync()
    {
      switch (BreakdownTester.Mode)
      {
        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.ACW:
          return await BreakdownTester.AcwManger.Time.GetRampTimeAsync();

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.DCW:
          return await BreakdownTester.DcwManger.Time.GetRampTimeAsync();

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR:
          return await BreakdownTester.IrManger.Time.GetRampTimeAsync();

        default: return 0;
      }
    }

    public async Task<double> GetTestTimeAsync()
    {
      switch (BreakdownTester.Mode)
      {
        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.ACW:
          return await BreakdownTester.AcwManger.Time.GetTestTimeAsync();

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.DCW:
          return await BreakdownTester.DcwManger.Time.GetTestTimeAsync();

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR:
          return await BreakdownTester.IrManger.Time.GetTestTimeAsync();

        default: return 0;
      }
    }

    public async Task<(bool Success, string Message)> SetRampTimeAsync(double value, IUserInteractionService? userMessageService = null)
    {
      switch (BreakdownTester.Mode)
      {
        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.ACW:
          return await BreakdownTester.AcwManger.Time.SetRampTimeAsync(value, userMessageService);

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.DCW:
          return await BreakdownTester.DcwManger.Time.SetRampTimeAsync(value, userMessageService);

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR:
          return await BreakdownTester.IrManger.Time.SetRampTimeAsync(value, userMessageService);

        default: return (false, string.Empty);
      }
    }

    public async Task<(bool Success, string Message)> SetTestTimeAsync(double value, IUserInteractionService? userMessageService = null)
    {
      switch (BreakdownTester.Mode)
      {
        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.ACW:
          return await BreakdownTester.AcwManger.Time.SetTestTimeAsync(value, userMessageService);

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.DCW:
          return await BreakdownTester.DcwManger.Time.SetTestTimeAsync(value, userMessageService);

        case Core.Shared.Metadata.Enums.DeviceEnums.BreakdownTypeMode.IR:
          return await BreakdownTester.IrManger.Time.SetTestTimeAsync(value, userMessageService);

        default: return (false, string.Empty);
      }
    }
  }
}
