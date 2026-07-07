using System.ComponentModel;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Перечисляет укрупнённые модули самоконтроля старого тестера АСК.
/// Значения соответствуют ключам /tests старой программы MKI.
/// </summary>
public enum LegacyAskSelfControlModule
{
  /// <summary>
  /// Самоконтроль цифрового вольтметра.
  /// </summary>
  [Description("цифровой вольтметр")]
  DigitalVoltmeter = 1,

  /// <summary>
  /// Самоконтроль АЦП.
  /// </summary>
  [Description("АЦП")]
  Adc = 2,

  /// <summary>
  /// Самоконтроль коммутации измерительных устройств и ПИНТов к шинам.
  /// </summary>
  [Description("коммутация устройств")]
  DeviceSwitching = 3,

  /// <summary>
  /// Самоконтроль ПИНТов.
  /// </summary>
  [Description("ПИНТы")]
  Pints = 4,

  /// <summary>
  /// Самоконтроль коммутатора.
  /// </summary>
  [Description("Коммутатор")]
  Commutator = 5,

  /// <summary>
  /// Самоконтроль ППУ.
  /// </summary>
  [Description("ППУ")]
  Ppu = 6,

  /// <summary>
  /// Самоконтроль ПКИ.
  /// </summary>
  [Description("ПКИ")]
  Pki = 7,

  /// <summary>
  /// Самоконтроль таймера.
  /// </summary>
  [Description("таймер")]
  Timer = 8
}
