using Ask.Core.Services.Errors.Models;

namespace Ask.Core.Shared.DTO.Input
{
  /// <summary>
  /// Содержит совокупный результат проверки пользовательского ввода.
  /// </summary>
  public sealed class InputValidationResult
  {
    /// <summary>
    /// Ошибки, обнаруженные при проверке.
    /// </summary>
    public IReadOnlyList<ErrorItem> Errors { get; }

    /// <summary>
    /// Признак отсутствия ошибок.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Создаёт результат проверки пользовательского ввода.
    /// </summary>
    /// <param name="errors">Обнаруженные ошибки.</param>
    public InputValidationResult(IEnumerable<ErrorItem> errors)
    {
      Errors = errors.ToArray();
    }
  }
}
