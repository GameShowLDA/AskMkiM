using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Protocol.Messages.Models;

/// <summary>
/// Содержит накопленные сообщения об ошибках и информационные сообщения алгоритма проверки.
/// </summary>
public sealed class AlgorithmExecutionResult
{
  /// <summary>
  /// Создаёт пустой результат выполнения алгоритма.
  /// </summary>
  public AlgorithmExecutionResult()
    : this(new List<ShowMessageModel>(), new List<ShowMessageModel>())
  {
  }

  /// <summary>
  /// Сообщения об ошибках алгоритма.
  /// </summary>
  public List<ShowMessageModel> Errors { get; }

  /// <summary>
  /// Информационные сообщения алгоритма.
  /// </summary>
  public List<ShowMessageModel> Info { get; }

  /// <summary>
  /// Создаёт результат алгоритма из заданных коллекций сообщений.
  /// </summary>
  /// <param name="errors">Сообщения об ошибках алгоритма.</param>
  /// <param name="info">Информационные сообщения алгоритма.</param>
  public AlgorithmExecutionResult(
    List<ShowMessageModel> errors,
    List<ShowMessageModel> info)
  {
    ArgumentNullException.ThrowIfNull(errors);
    ArgumentNullException.ThrowIfNull(info);

    Errors = errors;
    Info = info;
  }

  /// <summary>
  /// Добавляет сообщения другого результата алгоритма.
  /// </summary>
  /// <param name="other">Добавляемый результат алгоритма.</param>
  public void AddRange(AlgorithmExecutionResult? other)
  {
    if (other == null)
    {
      return;
    }

    Errors.AddRange(other.Errors);
    Info.AddRange(other.Info);
  }

  /// <summary>
  /// Создаёт результат, содержащий только сообщения об ошибках.
  /// </summary>
  /// <param name="errors">Сообщения об ошибках алгоритма.</param>
  /// <returns>Результат алгоритма без информационных сообщений.</returns>
  public static AlgorithmExecutionResult FromErrors(List<ShowMessageModel> errors)
  {
    ArgumentNullException.ThrowIfNull(errors);
    return new AlgorithmExecutionResult(errors, new List<ShowMessageModel>());
  }
}
