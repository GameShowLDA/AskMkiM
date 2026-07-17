using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Самоконтроль таймера старого тестера АСК.
/// </summary>
public sealed class LegacyAskTimerSelfControlTest : LegacyAskModuleTestBase
{
  /// <inheritdoc />
  public override LegacyAskSelfControlModule Module => LegacyAskSelfControlModule.Timer;

  /// <inheritdoc />
  protected override Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    return base.ValidateConfigurationAsync(context);
  }

  /// <inheritdoc />
  protected override string GetTestName(LegacyAskSelfControlContext context)
  {
    return "Тест таймера";
  }

  /// <inheritdoc />
  protected override async Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    DateTime startedAt = DateTime.Now;
    var stopwatch = Stopwatch.StartNew();
    ResetSummary();

    var controller = context.Devices.Controller;
    string title = GetTestName(context);
    int[] beforeMs = [50, 100, 150, 200, 250, 500];
    int[] impulseMs = [20, 50, 100, 200, 500, 1000];

    await context.Reporter.BeginSubTestAsync(title, 1, "Проверка времени до пуска и длительности импульса");
    for (int i = 0; i < impulseMs.Length; i++)
    {
      await PrepareTimerSignalAsync(context, controller);
      await context.Devices.Timer.SetStopAsync(controller, CalculateAdcThreshold(3.0, isLessThan: true), context.CancellationToken);
      await context.Devices.Timer.StartAsync(controller, CalculateAdcThreshold(6.0, isLessThan: false), context.CancellationToken);

      if (!ExecutionConfig.GetIsIdleModeEnabled())
      {
        await Task.Delay(beforeMs[i], context.CancellationToken);
      }
      await LegacyAskSelfTestFormat.SetPintBusesAsync(context, controller, 4, LegacyAskBus.A1, LegacyAskBus.B1);
      if (!ExecutionConfig.GetIsIdleModeEnabled())
      {
        await Task.Delay(impulseMs[i], context.CancellationToken);
      }
      await LegacyAskSelfTestFormat.SetPintBusesAsync(context, controller, 4, 0, LegacyAskBus.B1);

      await WaitTimerReadyAsync(context, controller);
      double measuredBefore = await ReadTimerMillisecondsAsync(context, controller, offset: 2, expectedMilliseconds: beforeMs[i]);
      double measuredImpulse = await ReadTimerMillisecondsAsync(context, controller, offset: 0, expectedMilliseconds: impulseMs[i]);
      measuredBefore = Math.Max(0, measuredBefore - 5.0 + context.Profile.HardwareAux.TdobTdo);
      measuredImpulse = Math.Max(0, measuredImpulse + context.Profile.HardwareAux.TdobTi);

      bool beforePassed = Math.Abs(measuredBefore - beforeMs[i]) <= 10.0;
      bool impulsePassed = Math.Abs(measuredImpulse - impulseMs[i]) <= 10.0;
      await context.Reporter.TestStepAsync($"Тест {i + 1}: до={beforeMs[i]}мс tи={impulseMs[i]}мс  tдо д.быть={beforeMs[i]}мс+-10мс  Tизм={measuredBefore:0.#}мс", beforePassed);
      await context.Reporter.TestStepAsync($"Тест {i + 1}: до={beforeMs[i]}мс tи={impulseMs[i]}мс  tи д.быть={impulseMs[i]}мс+-10мс  Tизм={measuredImpulse:0.#}мс", impulsePassed);
    }

    await context.Reporter.EndSubTestAsync(title, 1, "Проверка времени до пуска и длительности импульса");

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }

  /// <summary>
  /// Подготавливает ПИНТ4 и АЦП для проверки таймера контроллера.
  /// </summary>
  private static async Task PrepareTimerSignalAsync(LegacyAskSelfControlContext context, IAskMkiController controller)
  {
    await LegacyAskSelfTestFormat.SetPintOutputAsync(context, controller, 4, 8.0, 0.050, 0, LegacyAskBus.B1);
    await LegacyAskSelfTestFormat.SetAcpModeAsync(context, controller, LegacyAskAcpMode.Voltage10V, LegacyAskBus.A1, LegacyAskBus.B1);
  }

  /// <summary>
  /// Переводит порог напряжения таймера в код АЦП для старого контроллера.
  /// </summary>
  private static ushort CalculateAdcThreshold(double volts, bool isLessThan)
  {
    ushort value = (ushort)Math.Clamp((int)Math.Round(volts / (10.0 * 1.1) * 1000.0), 0, 0x03FF);
    return isLessThan ? (ushort)(value | 0x8000) : value;
  }

  /// <summary>
  /// Ожидает признак готовности таймера в боевом режиме.
  /// </summary>
  private static async Task WaitTimerReadyAsync(LegacyAskSelfControlContext context, IAskMkiController controller)
  {
    if (ExecutionConfig.GetIsIdleModeEnabled())
    {
      return;
    }

    var deadline = DateTime.UtcNow.AddSeconds(3);
    while (DateTime.UtcNow < deadline)
    {
      ushort ready = await context.Devices.Timer.ReadReadyAsync(controller, 1, context.CancellationToken);
      if ((ready & 0x0008) != 0)
      {
        return;
      }

      await Task.Delay(20, context.CancellationToken);
    }
  }

  /// <summary>
  /// Читает 32-битный счетчик таймера и переводит его тики в миллисекунды.
  /// </summary>
  private static async Task<double> ReadTimerMillisecondsAsync(
    LegacyAskSelfControlContext context,
    IAskMkiController controller,
    ushort offset,
    int expectedMilliseconds)
  {
    if (ExecutionConfig.GetIsIdleModeEnabled())
    {
      return expectedMilliseconds;
    }

    ushort lo = await context.Devices.Timer.ReadWordAsync(controller, offset, context.CancellationToken);
    ushort hi = await context.Devices.Timer.ReadWordAsync(controller, (ushort)(offset + 1), context.CancellationToken);
    uint ticks = lo | ((uint)hi << 16);
    double scale = context.Profile.HardwareAux.KmulKi > 0 ? context.Profile.HardwareAux.KmulKi : 1.0;
    return ticks * 0.1 * scale;
  }
}
