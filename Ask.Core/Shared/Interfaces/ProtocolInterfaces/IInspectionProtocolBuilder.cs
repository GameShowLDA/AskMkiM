using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.ExecutionEnums;

namespace Ask.Core.Shared.Interfaces.ProtocolInterfaces;

/// <summary>
/// Формирует текст итогового протокола по результатам завершённого действия.
/// </summary>
public interface IInspectionProtocolBuilder
{
  /// <summary>
  /// Строит итоговый текст для указанных настроек и накопленных ошибок.
  /// </summary>
  /// <param name="settings">Настройки и результаты завершённого действия.</param>
  /// <returns>Готовый текст итогового протокола.</returns>
  string Build(ActionSettings settings, ExecutionCompletionStatus completionStatus);
}
