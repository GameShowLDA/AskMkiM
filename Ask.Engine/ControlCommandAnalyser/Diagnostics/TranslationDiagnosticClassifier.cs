using Ask.Core.Services.Errors.Models;

namespace Ask.Engine.ControlCommandAnalyser.Diagnostics
{
  /// <summary>
  /// Классифицирует диагностики транслятора, которые зависят от состава,
  /// конфигурации или предельных возможностей оборудования.
  /// </summary>
  public static class TranslationDiagnosticClassifier
  {
    private static readonly HashSet<string> EquipmentCodes = new(
      StringComparer.OrdinalIgnoreCase)
    {
      nameof(ErrorCode.Gen_FastMeterNotFound),
      nameof(ErrorCode.Pr_EquipmentOutOfRange),
      nameof(ErrorCode.Ne_EquipmentOutOfRange),
      nameof(ErrorCode.Vsh_InvalidVshBusStructure),
      nameof(ErrorCode.Vsh_NoneVshBusStructure),
      nameof(ErrorCode.Vsh_DuplicateStand),
      nameof(ErrorCode.Vsh_InvalidRackNumber)
    };

    private static readonly string[] EquipmentDescriptionMarkers =
    {
      "MachineAddressNotConfigured",
      "LegacyCompatibilityAddress",
      "не найден быстрый измеритель",
      "не найдена пробойная установка",
      "не найден в конфигурации",
      "отсутствует в конфигурации",
      "не поддерживает шину",
      "пробойной установки"
    };

    public static bool IsEquipmentRelated(IDisplayIssue issue)
    {
      ArgumentNullException.ThrowIfNull(issue);

      string code = issue.CodeString ?? string.Empty;
      if (EquipmentCodes.Contains(code) ||
          code.StartsWith("Equipment_", StringComparison.OrdinalIgnoreCase) ||
          code.Contains("_Equipment", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }

      string description = issue.Description ?? string.Empty;
      return EquipmentDescriptionMarkers.Any(marker =>
        description.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
  }
}
