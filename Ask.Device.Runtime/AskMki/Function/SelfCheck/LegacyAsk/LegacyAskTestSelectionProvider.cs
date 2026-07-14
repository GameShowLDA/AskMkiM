using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;

namespace Ask.Engine.Tests.LegacyAsk;

/// <summary>
/// Предоставляет выбранную стойку АСК и выбранный legacy-тест для исполнителя.
/// </summary>
public interface ILegacyAskTestSelectionProvider
{
  /// <summary>
  /// Возвращает выбранную стойку тестера АСК.
  /// </summary>
  /// <returns>Выбранная стойка или <see langword="null"/>, если стойка не выбрана.</returns>
  IChassisManager? GetSelectedChassis();

  /// <summary>
  /// Возвращает выбранный тест старой АСК.
  /// </summary>
  /// <returns>Выбранный тест или <see langword="null"/>, если тест не выбран.</returns>
  LegacyAskTestDescriptor? GetSelectedTest();

  /// <summary>
  /// Возвращает вводные параметры выбранного теста.
  /// </summary>
  /// <returns>Словарь значений формы запуска, введенных пользователем.</returns>
  IReadOnlyDictionary<string, string> GetInputParameters();
}
