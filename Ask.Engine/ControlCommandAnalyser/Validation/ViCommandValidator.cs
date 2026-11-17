using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Validation;

/// <summary>
/// Проверяет совместимость шин команды ВИ со структурой ВШ.
/// </summary>
internal static class ViCommandValidator
{
  public static void ValidateVshCompatibility(IReadOnlyList<BaseCommandModel> models)
  {
    foreach (var vi in models.OfType<ViCommandModel>())
    {
      int? maximumPairNumber = GetMaximumPairNumber(models, vi.PowerSupplyChassisNumber);
      if (!maximumPairNumber.HasValue)
        continue;

      int pairNumber = GetPairNumber(vi);
      if (pairNumber == 0 || pairNumber <= maximumPairNumber.Value)
        continue;

      vi.Errors.Add(ViErrors.InvalidBus(
        vi.StartLineNumber,
        $"{vi.CommandNumber} {vi.Mnemonic}",
        $"Пара шин A{pairNumber}/B{pairNumber} недоступна для текущей структуры ВШ."));
    }
  }

  private static int? GetMaximumPairNumber(
    IEnumerable<BaseCommandModel> models,
    int? powerSupplyChassisNumber)
  {
    if (!powerSupplyChassisNumber.HasValue)
      return null;

    var structures = models
      .OfType<VshCommandModel>()
      .SelectMany(model => model.BusStructure ?? new Dictionary<BusStructureEnum.Type, List<int?>>())
      .Where(item => item.Value.Contains(powerSupplyChassisNumber.Value))
      .Select(item => item.Key)
      .ToArray();

    if (structures.Length == 0 || structures.Contains(BusStructureEnum.Type.BusCombined))
      return null;

    int maximumBusCount = structures.Max(structure => structure switch
    {
      BusStructureEnum.Type.Bus2 => 2,
      BusStructureEnum.Type.Bus4 => 4,
      BusStructureEnum.Type.Bus6 => 6,
      BusStructureEnum.Type.Bus8 => 8,
      _ => 0
    });
    return maximumBusCount > 0 ? maximumBusCount / 2 : null;
  }

  private static int GetPairNumber(ViCommandModel model)
  {
    string? bus = model.PositiveBus?.ToString() ?? model.NegativeBus?.ToString();
    return bus is not null && int.TryParse(bus[^1].ToString(), out int number) ? number : 0;
  }
}
