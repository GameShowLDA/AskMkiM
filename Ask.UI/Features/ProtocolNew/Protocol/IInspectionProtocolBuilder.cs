using Ask.Core.Shared.DTO.Executor;

namespace Ask.UI.Features.ProtocolNew.Protocol;

/// <summary>
/// Формирует текст итогового протокола по результатам завершённого действия.
/// </summary>
internal interface IInspectionProtocolBuilder
{
  /// <summary>
  /// Строит итоговый текст для указанных настроек и накопленных ошибок.
  /// </summary>
  /// <param name="settings">Настройки и результаты завершённого действия.</param>
  /// <returns>Готовый текст итогового протокола.</returns>
  string Build(ActionSettings settings);
}
