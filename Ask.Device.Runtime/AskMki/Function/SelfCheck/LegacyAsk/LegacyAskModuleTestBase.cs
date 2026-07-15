using Ask.Core.Services.Config.LegacyMki;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Базовый класс нативного модульного самоконтроля старого тестера АСК.
/// </summary>
public abstract class LegacyAskModuleTestBase
{
  private DateTime _summaryStartedAt;
  private TimeSpan _summaryElapsed;
  private bool _summaryIsIdleMode;
  private bool _summaryReady;

  /// <summary>
  /// Возвращает модуль, который проверяет тест.
  /// </summary>
  public abstract LegacyAskSelfControlModule Module { get; }

  /// <summary>
  /// Выполняет самоконтроль модуля.
  /// </summary>
  /// <param name="context">Контекст выполнения самоконтроля.</param>
  public async Task ExecuteAsync(LegacyAskSelfControlContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    context.CancellationToken.ThrowIfCancellationRequested();

    string testName = GetTestName(context);
    bool hasErrors = false;

    await context.Reporter.BeginTestAsync(testName);

    try
    {
      await ValidateConfigurationAsync(context);

      if (!LegacyAskSelfControlAvailability.IsAvailable(context.Profile, Module))
      {
        await context.Reporter.ErrorAsync(LegacyAskSelfControlModuleMetadata.GetUnavailableReason(Module));
        await context.Reporter.EndTestAsync(testName);
        await context.Reporter.CompleteCommandAsync(hasErrors: true);
        return;
      }

      hasErrors = !await ExecuteHardwareAsync(context) || context.Reporter.HasFailedMeasurements;
    }
    catch (OperationCanceledException)
    {
      hasErrors = true;
      await context.Reporter.ErrorAsync("Тест прерван пользователем");
    }
    catch (LegacyMkiHardwareProfileValidationException ex)
    {
      hasErrors = true;
      await context.Reporter.ErrorAsync("Ошибка конфигурации аппаратуры АСК: " + ex.Message);
    }
    catch (Exception ex) when (ex is LegacyAskProtocolException or TimeoutException or IOException or InvalidOperationException or UnauthorizedAccessException)
    {
      hasErrors = true;
      await context.Reporter.ErrorAsync($"Ошибка обмена с контроллером АСК: {ex.Message}");
    }

    await context.Reporter.EndTestAsync(testName);
    await AfterTestEndedAsync(context, testName, hasErrors);
    await context.Reporter.CompleteCommandAsync(hasErrors);
  }

  /// <summary>
  /// Проверяет параметры конфигурации, необходимые выбранному модулю.
  /// </summary>
  /// <param name="context">Контекст выполнения самоконтроля.</param>
  protected virtual Task ValidateConfigurationAsync(LegacyAskSelfControlContext context)
  {
    LegacyMkiHardwareProfileValidator.ThrowIfInvalid(context.Profile);
    return Task.CompletedTask;
  }

  /// <summary>
  /// Возвращает название теста для протокола.
  /// </summary>
  /// <param name="context">Контекст выполнения самоконтроля.</param>
  protected virtual string GetTestName(LegacyAskSelfControlContext context)
  {
    return LegacyAskSelfControlModuleMetadata.GetDisplayName(Module);
  }

  /// <summary>
  /// Выполняет дополнительные действия после закрытия основной строки <c>$TST</c>.
  /// </summary>
  /// <param name="context">Контекст выполнения самоконтроля.</param>
  /// <param name="testName">Название завершённого теста.</param>
  /// <param name="hasErrors">Признак завершения с ошибкой.</param>
  protected virtual Task AfterTestEndedAsync(LegacyAskSelfControlContext context, string testName, bool hasErrors)
  {
    return _summaryReady
      ? context.Reporter.WriteSummaryAsync(testName, _summaryIsIdleMode, _summaryStartedAt, _summaryElapsed, hasErrors)
      : Task.CompletedTask;
  }

  /// <summary>
  /// Запоминает параметры итоговой таблицы старой MKI.
  /// </summary>
  /// <param name="startedAt">Время начала выполнения теста.</param>
  /// <param name="elapsed">Длительность выполнения теста.</param>
  /// <param name="isIdleMode">Признак холостого режима.</param>
  protected void SetSummary(DateTime startedAt, TimeSpan elapsed, bool isIdleMode)
  {
    _summaryStartedAt = startedAt;
    _summaryElapsed = elapsed;
    _summaryIsIdleMode = isIdleMode;
    _summaryReady = true;
  }

  /// <summary>
  /// Сбрасывает состояние итоговой таблицы перед новым запуском.
  /// </summary>
  protected void ResetSummary()
  {
    _summaryReady = false;
  }

  /// <summary>
  /// Выполняет аппаратную часть проверки или эмулирует ответы в холостом режиме.
  /// </summary>
  /// <param name="context">Контекст выполнения самоконтроля.</param>
  /// <returns><see langword="true"/>, если проверка прошла без ошибок.</returns>
  protected virtual Task<bool> ExecuteHardwareAsync(LegacyAskSelfControlContext context)
  {
    throw new NotSupportedException($"Для модуля \"{LegacyAskSelfControlModuleMetadata.GetDisplayName(Module)}\" не реализован нативный алгоритм самоконтроля.");
  }
}
