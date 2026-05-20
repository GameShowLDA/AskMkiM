using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Parser;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Validation;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

public sealed class ControlAddressTranslationEngine
{
  private readonly AddressExpansionService expansionService;
  private readonly RmSemanticValidator validator;
  private readonly RmTranslationOptions options;

  public ControlAddressTranslationEngine(RmTranslationOptions? options = null)
  {
    this.options = options ?? RmTranslationOptions.Default;
    expansionService = new AddressExpansionService();
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
        entries.Add(new AddressMapping(
          objectAddresses[i],
          machineAddresses[i],
          synonyms?[i],
          mapping.Span));
      }
    }

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
}
