using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
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

    using var controller = new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(context.Profile, isIdleMode));
    string title = GetTestName(context);
    int[] beforeMs = [50, 100, 150, 200, 250, 500];
    int[] impulseMs = [20, 50, 100, 200, 500, 1000];

    await context.Reporter.BeginSubTestAsync(title, 1, "Проверка времени до пуска и длительности импульса");
    for (int i = 0; i < impulseMs.Length; i++)
    {
      await controller.SetTimerStopAsync((ushort)impulseMs[i], context.CancellationToken);
      await controller.StartTimerAsync((ushort)beforeMs[i], context.CancellationToken);
      await controller.ReadTimerReadyAsync(1, context.CancellationToken);
      await controller.ReadTimerWordAsync(0, context.CancellationToken);
      await context.Reporter.TestStepAsync($"Тест {i + 1}: до={beforeMs[i]}мс tи={impulseMs[i]}мс  tдо={beforeMs[i]}мс+-10мс  Tизм={impulseMs[i]}мс+-10мс");
    }

    await context.Reporter.EndSubTestAsync(title, 1, "Проверка времени до пуска и длительности импульса");

    stopwatch.Stop();
    SetSummary(startedAt, stopwatch.Elapsed, isIdleMode);
    return true;
  }
}
