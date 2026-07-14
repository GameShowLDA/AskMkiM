namespace Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

/// <summary>
/// Функции бинарного протокола контроллера старого тестера АСК.
/// </summary>
public enum LegacyAskFunction : byte
{
  /// <summary>
  /// Чтение результата АЦП.
  /// </summary>
  ReadAdc = 0x99,

  /// <summary>
  /// Проверка подключения электронной точки.
  /// </summary>
  CheckElectronicConnection = 0xA0,

  /// <summary>
  /// Проверка размыкания электронной точки.
  /// </summary>
  CheckElectronicDisconnection = 0xA1,

  /// <summary>
  /// Проверка отсутствия лишнего подключения электронной точки.
  /// </summary>
  CheckNoElectronicConnection = 0xA2,

  /// <summary>
  /// Запись регистра команд.
  /// </summary>
  WriteCommandRegister = 0xA3,

  /// <summary>
  /// Запись подключения регистра команд к шине.
  /// </summary>
  WriteBusCommand = 0xA4,

  /// <summary>
  /// Запись порога остановки таймера АЦП.
  /// </summary>
  SetTimerStop = 0xA6,

  /// <summary>
  /// Запуск таймера АЦП.
  /// </summary>
  StartTimer = 0xA7,

  /// <summary>
  /// Чтение готовности таймера АЦП.
  /// </summary>
  ReadTimerReady = 0xA8,

  /// <summary>
  /// Чтение слова результата таймера АЦП.
  /// </summary>
  ReadTimerWord = 0xA9,

  /// <summary>
  /// Запись количества стробов.
  /// </summary>
  WriteStrobeCount = 0xAA,

  /// <summary>
  /// Настройка параметров строба.
  /// </summary>
  SetStrobe = 0xAB
}

/// <summary>
/// Регистры контроллера старого тестера АСК, доступные через общий протокол чтения и записи.
/// </summary>
public enum LegacyAskRegister : ushort
{
  /// <summary>
  /// Регистр команд.
  /// </summary>
  Command = 0x300,

  /// <summary>
  /// Регистр адреса.
  /// </summary>
  Address = 0x302,

  /// <summary>
  /// Регистр включения приборов старого контроллера АСК.
  /// </summary>
  DevicePower = 0x30C,

  /// <summary>
  /// Регистр ПИНТ4.
  /// </summary>
  Gui3 = 0x31E,

  /// <summary>
  /// Регистр ПИНТ4.
  /// </summary>
  Gui4 = 0x320,

  /// <summary>
  /// Регистр подключения шин к цифровому вольтметру.
  /// </summary>
  V7Gate = 0x326,

  /// <summary>
  /// Регистр режима цифрового вольтметра.
  /// </summary>
  V7Mode = 0x328,

  /// <summary>
  /// Регистр режимов ППУ MKI.
  /// </summary>
  PpuMkiMode = 0x32A,

  /// <summary>
  /// Регистр команд ППУ MKI.
  /// </summary>
  PpuMkiCommand = 0x32C,

  /// <summary>
  /// Регистр режимов АЦП.
  /// </summary>
  AcpMode = 0x330,

  /// <summary>
  /// Регистр данных АЦП.
  /// </summary>
  AcpData = 0x332,

  /// <summary>
  /// Регистр подключения шин АЦП.
  /// </summary>
  AcpGate = 0x334,

  /// <summary>
  /// Программный регистр старта АЦП.
  /// </summary>
  AcpStart = 0x338,

  /// <summary>
  /// Программный регистр стопа АЦП.
  /// </summary>
  AcpStop = 0x33A,

  /// <summary>
  /// Виртуальный регистр прерываний и состояния питания.
  /// </summary>
  Interrupt = 0x33C
}

/// <summary>
/// Биты шин старой АСК из <c>mkhead.h</c>.
/// </summary>
public static class LegacyAskBus
{
  /// <summary>
  /// Шина A1.
  /// </summary>
  public const ushort A1 = 0x0001;

  /// <summary>
  /// Шина B1.
  /// </summary>
  public const ushort B1 = 0x0002;

  /// <summary>
  /// Шина A2.
  /// </summary>
  public const ushort A2 = 0x0004;

  /// <summary>
  /// Шина B2.
  /// </summary>
  public const ushort B2 = 0x0008;

  /// <summary>
  /// Признак подключения общего входа к земле/источнику.
  /// </summary>
  public const ushort GroundSource = 0x0100;
}

/// <summary>
/// Расширенные биты шин старой АСК из <c>mkhead.h</c>.
/// </summary>
public static class LegacyAskExtendedBus
{
  /// <summary>
  /// Шина A3.
  /// </summary>
  public const ushort A3 = 0x0010;

  /// <summary>
  /// Шина B3.
  /// </summary>
  public const ushort B3 = 0x0020;

  /// <summary>
  /// Шина A4.
  /// </summary>
  public const ushort A4 = 0x0040;

  /// <summary>
  /// Шина B4.
  /// </summary>
  public const ushort B4 = 0x0080;

  /// <summary>
  /// Верхняя электронная шина ЭВ.
  /// </summary>
  public const ushort Ev = 0x0100;

  /// <summary>
  /// Нижняя электронная шина ЭН.
  /// </summary>
  public const ushort En = 0x0200;
}

/// <summary>
/// Биты регистра команд старого контроллера АСК.
/// </summary>
public static class LegacyAskCommandBits
{
  /// <summary>
  /// Подключение к шине A релейного коммутатора.
  /// </summary>
  public const ushort RelayA = 0x0001;

  /// <summary>
  /// Подключение к шине B релейного коммутатора.
  /// </summary>
  public const ushort RelayB = 0x0002;

  /// <summary>
  /// Подключение к верхней электронной шине ЭВ.
  /// </summary>
  public const ushort ElectronicTop = 0x0004;

  /// <summary>
  /// Подключение к нижней электронной шине ЭН.
  /// </summary>
  public const ushort ElectronicBottom = 0x0008;

  /// <summary>
  /// Параллельный режим контроллера.
  /// </summary>
  public const ushort ParallelMode = 0x0020;

  /// <summary>
  /// Включение групповых реле.
  /// </summary>
  public const ushort GroupRelay = 0x0040;

  /// <summary>
  /// Электронная прозвонка.
  /// </summary>
  public const ushort ElectronicProbe = 0x0200;

  /// <summary>
  /// Подключение плюса ПИНТ4 к ЭВ.
  /// </summary>
  public const ushort Gui4PlusToEv = 0x0400;

  /// <summary>
  /// Подключение минуса ПИНТ4 к ЭН.
  /// </summary>
  public const ushort Gui4MinusToEn = 0x0800;

  /// <summary>
  /// Отключение групповых реле.
  /// </summary>
  public const ushort DisableGroupRelay = 0x1000;

  /// <summary>
  /// Подключение источника тока АЦП к ЭВ/ЭН.
  /// </summary>
  public const ushort AdcCurrentSourceToElectronicBus = 0x2000;

  /// <summary>
  /// Результат электронной прозвонки.
  /// </summary>
  public const ushort ElectronicProbeResult = 0x8000;
}

/// <summary>
/// Подрегистры ПИНТа в старом составном регистре MKI.
/// </summary>
public static class LegacyAskPintSubRegister
{
  /// <summary>
  /// Уставка напряжения.
  /// </summary>
  public const byte Voltage = 1;

  /// <summary>
  /// Уставка тока.
  /// </summary>
  public const byte Current = 2;

  /// <summary>
  /// Подключение плюсового выхода к шинам.
  /// </summary>
  public const byte PositiveBus = 3;

  /// <summary>
  /// Подключение минусового выхода к шинам.
  /// </summary>
  public const byte NegativeBus = 4;
}

/// <summary>
/// Режимы АЦП из <c>acp.h</c> старой MKI.
/// </summary>
public static class LegacyAskAcpMode
{
  /// <summary>
  /// Измерение напряжения, предел 100 В.
  /// </summary>
  public const ushort Voltage100V = 0x1000;

  /// <summary>
  /// Измерение напряжения, предел 10 В.
  /// </summary>
  public const ushort Voltage10V = 0x1400;

  /// <summary>
  /// Измерение напряжения, предел 1 В.
  /// </summary>
  public const ushort Voltage1V = 0x1800;

  /// <summary>
  /// Источник тока АЦП с ограничением 4 В.
  /// </summary>
  public const ushort CurrentSource4V = 0x25B0;

  /// <summary>
  /// Источник тока АЦП с ограничением 11 В.
  /// </summary>
  public const ushort CurrentSource11V = 0x25D0;

  /// <summary>
  /// Режим сопротивления около 0.1 кОм.
  /// </summary>
  public const ushort Resistance100Ohm = 0x29B0;

  /// <summary>
  /// Режим сопротивления около 1 кОм.
  /// </summary>
  public const ushort Resistance1KOhm = 0x25D0;

  /// <summary>
  /// Режим сопротивления около 10 кОм.
  /// </summary>
  public const ushort Resistance10KOhm = 0x2550;

  /// <summary>
  /// Режим сопротивления около 100 кОм.
  /// </summary>
  public const ushort Resistance100KOhm = 0x24D0;
}

/// <summary>
/// Режимы ППУ, совпадающие с абстрактными битами старой MKI.
/// </summary>
