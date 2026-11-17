using Ask.Core.Services.Errors.Models;

namespace Ask.Core.Services.Errors.Translation;

/// <summary>
/// Формирует ошибки трансляции команды ВИ.
/// </summary>
public static class ViErrors
{
  public static ErrorItem InvalidBody(int line, string command, string? fragment = null) => Create(
    ErrorCode.Vi_InvalidBody, line, command,
    "Ожидается один блок *...* с параметрами команды ВИ.", fragment);

  public static ErrorItem InvalidPowerSupplyNumber(int line, string command, string? fragment) => Create(
    ErrorCode.Vi_InvalidPowerSupplyNumber, line, command,
    "Первым параметром должен быть указан номер ПИНТ от 1 до 4.", fragment);

  public static ErrorItem PowerSupplyNotFound(int line, string command, int number) => Create(
    ErrorCode.Vi_PowerSupplyNotFound, line, command,
    $"ПИНТ №{number} не найден в конфигурации.", number.ToString());

  public static ErrorItem AmbiguousPowerSupply(int line, string command, int number) => Create(
    ErrorCode.Vi_AmbiguousPowerSupply, line, command,
    $"Номер ПИНТ №{number} назначен нескольким источникам. Номер ПИНТ должен быть однозначным.", number.ToString());

  public static ErrorItem InvalidParameterCombination(int line, string command) => Create(
    ErrorCode.Vi_InvalidParameterCombination, line, command,
    "Допустимы: только номер; номер, напряжение и ток; либо эти же параметры с парой шин.");

  public static ErrorItem InvalidVoltage(int line, string command, string description, string? fragment = null) => Create(
    ErrorCode.Vi_InvalidVoltage, line, command, description, fragment);

  public static ErrorItem InvalidCurrent(int line, string command, string description, string? fragment = null) => Create(
    ErrorCode.Vi_InvalidCurrent, line, command, description, fragment);

  public static ErrorItem InvalidBus(int line, string command, string description, string? fragment = null) => Create(
    ErrorCode.Vi_InvalidBus, line, command, description, fragment);

  private static ErrorItem Create(
    ErrorCode code,
    int line,
    string command,
    string description,
    string? fragment = null) => new()
    {
      Code = code,
      SourceLineNumber = line,
      Command = command,
      Description = description,
      SourceFragment = fragment
    };
}
