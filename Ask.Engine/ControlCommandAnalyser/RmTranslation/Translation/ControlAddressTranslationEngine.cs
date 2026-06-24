using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Parser;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Validation;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

public sealed class ControlAddressTranslationEngine
{
  private readonly AddressExpansionService expansionService;
  private readonly ILegacyAddressMapper legacyAddressMapper;
  private readonly RmSemanticValidator validator;
  private readonly RmTranslationOptions options;

  public ControlAddressTranslationEngine(RmTranslationOptions? options = null)
  {
    this.options = options ?? RmTranslationOptions.Default;
    expansionService = new AddressExpansionService();
    legacyAddressMapper = this.options.LegacyAddressMapper ?? NoLegacyAddressMapper.Instance;
    validator = new RmSemanticValidator();
  }

  public Result<TranslationDocumentAst> Parse(string commandText)
  {
    return RmParser.Parse(commandText);
  }

  public TranslationResult Translate(string commandText)
  {
    var parseResult = Parse(commandText);
    if (parseResult.Value is null)
      return new TranslationResult(Array.Empty<AddressMapping>(), parseResult.Diagnostics);

    return Expand(parseResult.Value, parseResult.Diagnostics);
  }

  public TranslationResult Expand(TranslationDocumentAst ast)
  {
    return Expand(ast, Array.Empty<RmDiagnostic>());
  }

  public IReadOnlyList<RmDiagnostic> Validate(IReadOnlyList<AddressMapping> entries)
  {
    return validator.Validate(entries);
  }

  private TranslationResult Expand(
    TranslationDocumentAst ast,
    IReadOnlyList<RmDiagnostic> existingDiagnostics)
  {
    var diagnostics = new List<RmDiagnostic>(existingDiagnostics);
    var entries = new List<AddressMapping>();
    var failedAddresses = new List<MachineAddress>();

    foreach (var mapping in ast.Mappings)
    {
      var binding = BindExpressions(mapping);
      var objectResult = expansionService.ExpandObjectExpression(binding.ObjectExpression);
      var machineResult = expansionService.ExpandMachineExpression(mapping.Machine);
      diagnostics.AddRange(objectResult.Diagnostics);
      diagnostics.AddRange(machineResult.Diagnostics);

      Result<ObjectAddressRange>? synonymResult = null;
      if (binding.SynonymExpression is not null)
      {
        synonymResult = expansionService.ExpandObjectExpression(binding.SynonymExpression);
        diagnostics.AddRange(synonymResult.Diagnostics);
      }

      if (!objectResult.IsSuccess || !machineResult.IsSuccess || synonymResult?.IsSuccess == false)
        continue;

      var objectAddresses = objectResult.Value?.Addresses ?? Array.Empty<ObjectAddress>();
      var machineAddresses = machineResult.Value ?? Array.Empty<MachineAddress>();
      var synonyms = synonymResult?.Value?.Addresses;

      if (objectAddresses.Count != machineAddresses.Count)
      {
        diagnostics.Add(RmDiagnostic.Error(
          RmDiagnosticCode.RangeLengthMismatch,
          $"Диапазоны имеют разную длину: ОК={objectAddresses.Count}, машинные адреса={machineAddresses.Count}.",
          mapping.Span));
        continue;
      }

      if (synonyms is not null && synonyms.Count != objectAddresses.Count)
      {
        diagnostics.Add(RmDiagnostic.Error(
          RmDiagnosticCode.RangeLengthMismatch,
          $"Диапазоны имеют разную длину: синонимы={synonyms.Count}, ОК={objectAddresses.Count}.",
          mapping.Span));
        continue;
      }

      for (var i = 0; i < objectAddresses.Count; i++)
      {
        var mappedMachineAddress = legacyAddressMapper.Map(machineAddresses[i], mapping.Span);

        if (!mappedMachineAddress.IsSuccess || mappedMachineAddress.Address is null)
        {
          failedAddresses.Add(machineAddresses[i]);
          continue;
        }

        AddAddressRangeDiagnostic(failedAddresses, diagnostics, mapping.Span);

        entries.Add(new AddressMapping(
          objectAddresses[i],
          mappedMachineAddress.Address.Value,
          synonyms?[i],
          mapping.Span)
        {
          SourceMachineAddress = machineAddresses[i]
        });
      }
    }

    AddAddressRangeDiagnostic(failedAddresses, diagnostics, ast.Mappings.LastOrDefault()?.Span ?? default);

    diagnostics.AddRange(validator.Validate(entries));
    return new TranslationResult(entries, diagnostics);
  }

  private BoundMappingExpressions BindExpressions(AddressMappingSyntax mapping)
  {
    if (mapping.Middle is null)
      return new BoundMappingExpressions(mapping.Left, null);

    return options.SynonymBindingMode switch
    {
      SynonymBindingMode.SynonymThenObject => new BoundMappingExpressions(mapping.Middle, mapping.Left),
      _ => new BoundMappingExpressions(mapping.Left, mapping.Middle)
    };
  }

  private sealed record BoundMappingExpressions(
    AddressExpressionSyntax ObjectExpression,
    AddressExpressionSyntax? SynonymExpression);

  private static void AddAddressRangeDiagnostic(
    List<MachineAddress> failedAddresses,
    List<RmDiagnostic> diagnostics,
    TextSpan span)
  {
    if (failedAddresses.Count == 0)
      return;

    var first = failedAddresses[0];
    var last = failedAddresses[^1];
    var message = failedAddresses.Count == 1
      ? $"Адрес {first} не задан в конфигурации. Проверьте RelaySwitchModules и PointCount."
      : $"Адреса с {first} по {last} не заданы в конфигурации. Проверьте RelaySwitchModules и PointCount.";

    diagnostics.Add(RmDiagnostic.Error(
      RmDiagnosticCode.MachineAddressNotConfigured,
      message,
      span));

    failedAddresses.Clear();
  }
}
