namespace Ask.Device.ResponseProcessor.Multimeter.ResponseModels;

/// <summary>
/// Содержит код и описание ошибки, возвращённые мультиметром.
/// </summary>
public sealed class InstrumentErrorResponse
{
  /// <summary>
  /// Числовой код ошибки прибора.
  /// </summary>
  public int Code { get; init; }

  /// <summary>
  /// Описание ошибки прибора.
  /// </summary>
  public string Message { get; init; } = string.Empty;
}
