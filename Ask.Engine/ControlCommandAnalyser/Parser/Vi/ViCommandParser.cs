using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Vi;

/// <summary>
/// Разбирает команду ВИ и проверяет её параметры по конфигурации источника питания.
/// </summary>
internal sealed class ViCommandParser : ICommandParser
{
  private static readonly Regex CommandHeaderRegex = new(
    @"^\s*\d+\s+ВИ(?=\s|$)\s*(?<body>.*)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  private static readonly Regex ParameterBlockRegex = new(
    @"^\s*(?:(?<condition>Б)\s+)?\*(?<body>[^*]*)\*\s*$",
    RegexOptions.Compiled);

  private static readonly Regex VoltageRegex = new(
    @"^(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>[мМкК]?[вВ])$",
    RegexOptions.Compiled);

  private static readonly Regex CurrentRegex = new(
    @"^(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>[мМкК]?[аА])$",
    RegexOptions.Compiled);

  private static readonly Regex BusRegex = new(
    @"^(?<sign>[+-])\s*(?<bus>[АаAaВвBb][1-4])$",
    RegexOptions.Compiled);

  private readonly Func<IReadOnlyCollection<ViPowerSupplyLimits>> powerSupplyProvider;

  /// <summary>
  /// Создаёт парсер, использующий актуальную конфигурацию источников питания.
  /// </summary>
  public ViCommandParser()
    : this(LoadPowerSupplyLimits)
  {
  }

  internal ViCommandParser(Func<IReadOnlyCollection<ViPowerSupplyLimits>> powerSupplyProvider)
  {
    this.powerSupplyProvider = powerSupplyProvider ?? throw new ArgumentNullException(nameof(powerSupplyProvider));
  }

  /// <inheritdoc />
  public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.VI);

  /// <inheritdoc />
  public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
  {
    lines ??= new List<string>();
    var model = new ViCommandModel
    {
      CommandNumber = commandNumber,
      SourceLines = new List<string>(lines),
      StartLineNumber = numberLine
    };
    string command = $"{commandNumber} {mnemonic}";

    var processedLines = CommentsParser.ParseComments(new List<string>(lines), model);
    string source = string.Concat(processedLines).Trim();
    var headerMatch = CommandHeaderRegex.Match(source);
    string remainder = headerMatch.Success ? headerMatch.Groups["body"].Value : source;

    var blockMatch = ParameterBlockRegex.Match(remainder);
    if (!blockMatch.Success)
    {
      model.Errors.Add(ViErrors.InvalidBody(numberLine, command, remainder));
      return model;
    }

    if (blockMatch.Groups["condition"].Success)
      model.AlgorithmKey.Add("Б");

    string block = blockMatch.Groups["body"].Value;
    var tokens = block.Split(',', StringSplitOptions.TrimEntries);
    if (tokens.Length == 0 || tokens.Any(string.IsNullOrWhiteSpace))
    {
      model.Errors.Add(ViErrors.InvalidBody(numberLine, command, block));
      return model;
    }

    if (!int.TryParse(tokens[0], NumberStyles.None, CultureInfo.InvariantCulture, out int powerSupplyNumber)
        || powerSupplyNumber <= 0)
    {
      model.Errors.Add(ViErrors.InvalidPowerSupplyNumber(numberLine, command, tokens[0]));
      return model;
    }

    model.PowerSupplyNumber = powerSupplyNumber;
    var configuredSupply = ResolvePowerSupply(model, command, numberLine, powerSupplyNumber);

    if (tokens.Length == 1)
    {
      model.Operation = ViOperation.DisconnectAndRelease;
      return model;
    }

    int unrecognizedCount = 0;
    for (int index = 1; index < tokens.Length; index++)
    {
      string token = tokens[index];
      if (TryParseVoltage(token, out decimal voltage))
      {
        if (model.Voltage.HasValue)
        {
          unrecognizedCount++;
          model.Errors.Add(ViErrors.InvalidVoltage(
            numberLine, command, "Напряжение указано более одного раза.", token));
        }
        else
        {
          model.Voltage = voltage;
          model.VoltageSource = token;
        }
        continue;
      }

      if (TryParseCurrent(token, out decimal current))
      {
        if (model.Current.HasValue)
        {
          unrecognizedCount++;
          model.Errors.Add(ViErrors.InvalidCurrent(
            numberLine, command, "Ток указан более одного раза.", token));
        }
        else
        {
          model.Current = current;
          model.CurrentSource = token;
        }
        continue;
      }

      if (TryParseBus(token, out char sign, out SwitchingBus bus))
      {
        if (!ApplyBus(model, sign, bus, numberLine, command, token))
          unrecognizedCount++;
        continue;
      }

      unrecognizedCount++;
      model.Errors.Add(ViErrors.InvalidBody(numberLine, command, token));
    }

    ValidateCombination(model, numberLine, command, unrecognizedCount);
    ValidateBusPair(model, numberLine, command);

    if (configuredSupply.HasValue)
      ValidateElectricalParameters(model, configuredSupply.Value, numberLine, command);

    return model;
  }

  private ViPowerSupplyLimits? ResolvePowerSupply(
    ViCommandModel model,
    string command,
    int line,
    int powerSupplyNumber)
  {
    var matches = powerSupplyProvider()
      .Where(source => source.Number == powerSupplyNumber)
      .ToArray();

    if (matches.Length == 0)
    {
      model.Errors.Add(ViErrors.PowerSupplyNotFound(line, command, powerSupplyNumber));
      return null;
    }

    if (matches.Length > 1)
    {
      model.Errors.Add(ViErrors.AmbiguousPowerSupply(line, command, powerSupplyNumber));
      return null;
    }

    model.PowerSupplyChassisNumber = matches[0].NumberChassis;
    return matches[0];
  }

  private static void ValidateCombination(ViCommandModel model, int line, string command, int unrecognizedCount)
  {
    bool hasElectricalParameters = model.Voltage.HasValue && model.Current.HasValue;
    bool hasNoBuses = !model.PositiveBus.HasValue && !model.NegativeBus.HasValue;
    bool hasBusPair = model.PositiveBus.HasValue && model.NegativeBus.HasValue;

    if (unrecognizedCount == 0 && hasElectricalParameters && hasNoBuses)
    {
      model.Operation = ViOperation.ConfigureWithoutBuses;
      return;
    }

    if (unrecognizedCount == 0 && hasElectricalParameters && hasBusPair)
    {
      model.Operation = ViOperation.ConfigureAndConnect;
      return;
    }

    model.Errors.Add(ViErrors.InvalidParameterCombination(line, command));
  }

  private static void ValidateBusPair(ViCommandModel model, int line, string command)
  {
    if (!model.PositiveBus.HasValue && !model.NegativeBus.HasValue)
      return;

    if (!model.PositiveBus.HasValue || !model.NegativeBus.HasValue)
      return;

    string positive = model.PositiveBus.Value.ToString();
    string negative = model.NegativeBus.Value.ToString();
    bool isExpectedPair = positive.StartsWith('A')
      && negative.StartsWith('B')
      && positive[^1] == negative[^1];

    if (!isExpectedPair)
    {
      model.Errors.Add(ViErrors.InvalidBus(
        line,
        command,
        "Положительная и отрицательная шины должны образовывать пару +A1,-B1 … +A4,-B4."));
    }
  }

  private static void ValidateElectricalParameters(
    ViCommandModel model,
    ViPowerSupplyLimits supply,
    int line,
    string command)
  {
    if (model.Voltage is decimal voltage)
    {
      if (voltage < supply.MinVoltage || voltage > supply.MaxVoltage)
      {
        model.Errors.Add(ViErrors.InvalidVoltage(
          line,
          command,
          $"Напряжение {voltage} В выходит за рабочий диапазон {supply.MinVoltage}–{supply.MaxVoltage} В.",
          model.VoltageSource));
      }
      else if (!MatchesStep(voltage, supply.MinVoltage, supply.VoltageStep))
      {
        model.Errors.Add(ViErrors.InvalidVoltage(
          line,
          command,
          $"Напряжение {voltage} В не соответствует шагу {supply.VoltageStep} В от минимума {supply.MinVoltage} В.",
          model.VoltageSource));
      }
    }

    if (model.Current is decimal current)
    {
      if (current < supply.MinCurrent || current > supply.MaxCurrent)
      {
        model.Errors.Add(ViErrors.InvalidCurrent(
          line,
          command,
          $"Ток {current} А выходит за рабочий диапазон {supply.MinCurrent}–{supply.MaxCurrent} А.",
          model.CurrentSource));
      }
      else if (!MatchesStep(current, supply.MinCurrent, supply.CurrentStep))
      {
        model.Errors.Add(ViErrors.InvalidCurrent(
          line,
          command,
          $"Ток {current} А не соответствует шагу {supply.CurrentStep} А от минимума {supply.MinCurrent} А.",
          model.CurrentSource));
      }
    }
  }

  private static bool ApplyBus(
    ViCommandModel model,
    char sign,
    SwitchingBus bus,
    int line,
    string command,
    string source)
  {
    if (sign == '+')
    {
      if (model.PositiveBus.HasValue)
      {
        model.Errors.Add(ViErrors.InvalidBus(line, command, "Положительная шина указана более одного раза.", source));
        return false;
      }
      model.PositiveBus = bus;
      return true;
    }

    if (model.NegativeBus.HasValue)
    {
      model.Errors.Add(ViErrors.InvalidBus(line, command, "Отрицательная шина указана более одного раза.", source));
      return false;
    }
    model.NegativeBus = bus;
    return true;
  }

  private static bool TryParseVoltage(string source, out decimal value)
  {
    value = default;
    var match = VoltageRegex.Match(source);
    if (!match.Success || !TryParseDecimal(match.Groups["value"].Value, out decimal numericValue))
      return false;

    string unit = match.Groups["unit"].Value.ToUpperInvariant();
    value = unit switch
    {
      "МВ" => numericValue / 1000m,
      "КВ" => numericValue * 1000m,
      _ => numericValue
    };
    return true;
  }

  private static bool TryParseCurrent(string source, out decimal value)
  {
    value = default;
    var match = CurrentRegex.Match(source);
    if (!match.Success || !TryParseDecimal(match.Groups["value"].Value, out decimal numericValue))
      return false;

    string unit = match.Groups["unit"].Value.ToUpperInvariant();
    value = unit switch
    {
      "МА" => numericValue / 1000m,
      "КА" => numericValue * 1000m,
      _ => numericValue
    };
    return true;
  }

  private static bool TryParseBus(string source, out char sign, out SwitchingBus bus)
  {
    sign = default;
    bus = default;
    var match = BusRegex.Match(source);
    if (!match.Success)
      return false;

    sign = match.Groups["sign"].Value[0];
    return BusConverter.TryParseSwitchingBus(match.Groups["bus"].Value.ToUpperInvariant(), out bus)
      && !bus.ToString().StartsWith("AB", StringComparison.Ordinal);
  }

  private static bool TryParseDecimal(string source, out decimal value)
    => decimal.TryParse(
      source.Replace(',', '.'),
      NumberStyles.AllowDecimalPoint,
      CultureInfo.InvariantCulture,
      out value);

  private static bool MatchesStep(decimal value, decimal minimum, decimal step)
    => step > 0m && (value - minimum) % step == 0m;

  private static IReadOnlyCollection<ViPowerSupplyLimits> LoadPowerSupplyLimits()
    => PowerSupplies.GetAllAsync()
      .GetAwaiter()
      .GetResult()
      .Select(source => new ViPowerSupplyLimits(
        source.Number,
        source.NumberChassis,
        source.VoltageManager.MinVoltage,
        source.VoltageManager.MaxVoltage,
        source.VoltageManager.VoltageStep,
        source.CurrentManager.MinCurrent,
        source.CurrentManager.MaxCurrent,
        source.CurrentManager.CurrentStep))
      .ToArray();
}

/// <summary>
/// Содержит рабочие пределы источника, необходимые для трансляции ВИ.
/// </summary>
internal readonly record struct ViPowerSupplyLimits(
  int Number,
  int NumberChassis,
  decimal MinVoltage,
  decimal MaxVoltage,
  decimal VoltageStep,
  decimal MinCurrent,
  decimal MaxCurrent,
  decimal CurrentStep);
