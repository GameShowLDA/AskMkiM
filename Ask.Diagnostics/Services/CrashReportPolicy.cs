using Ask.Core.Services.Errors.Device;

namespace Ask.Diagnostics.Services;

/// <summary>
/// Определяет, требует ли исключение диагностического отчёта.
/// </summary>
public static class CrashReportPolicy
{
  /// <summary>
  /// Исключает ожидаемое отсутствие ответа оборудования из диагностики сбоев.
  /// </summary>
  /// <param name="exception">Исключение, переданное обработчику диагностики.</param>
  /// <returns><see langword="true"/>, если требуется отчёт об исключении.</returns>
  public static bool ShouldReport(Exception exception)
  {
    ArgumentNullException.ThrowIfNull(exception);

    return exception switch
    {
      DeviceNoResponseException => false,
      AggregateException aggregate when aggregate.InnerExceptions.Count > 0 =>
        aggregate.InnerExceptions.Any(ShouldReport),
      _ => true,
    };
  }
}
