using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Device.Runtime.Function.GPT
{
  /// <summary>
  /// Управляет параметрами времени испытания устройства.
  /// </summary>
  public class TimeManager : ITimeManager
  {
    /// <summary>
    /// Устройство для проведения испытания на пробой.
    /// </summary>
    private IBreakdownTester BreakdownTester { get; init; }

    /// <summary>
    /// Установленное целевое время испытания.
    /// </summary>
    private double Time = -1;

    /// <summary>
    /// Инициализирует экземпляр класса <see cref="TimeManager"/>.
    /// </summary>
    /// <param name="breakdownTester">Устройство для проведения испытания на пробой.</param>
    public TimeManager(IBreakdownTester breakdownTester)
    {
      BreakdownTester = breakdownTester;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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


    /// <summary>
    /// Устанавливает целевое время.
    /// </summary>
    /// <param name="time">Целевое время.</param>
    public void SetTargetTime(double time)
    {
      Time = time;
    }

    /// <summary>
    /// Возвращает установленное целевое время.
    /// </summary>
    /// <returns>Целевое время.</returns>
    public double GetTargetTime()
    {
      return Time;
    }
  }
}
