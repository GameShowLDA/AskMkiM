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
  /// Регистр ПИНТ4.
  /// </summary>
  Gui3 = 0x31E,

  /// <summary>
  /// Р РµРіРёСЃС‚СЂ РџРРќРў4.
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
  /// Р РµРіРёСЃС‚СЂ СЂРµР¶РёРјРѕРІ РџРџРЈ MKI.
  /// </summary>
  PpuMkiMode = 0x32A,

  /// <summary>
  /// Р РµРіРёСЃС‚СЂ РєРѕРјР°РЅРґ РџРџРЈ MKI.
  /// </summary>
  PpuMkiCommand = 0x32C,

  /// <summary>
  /// Р РµРіРёСЃС‚СЂ СЂРµР¶РёРјРѕРІ РђР¦Рџ.
  /// </summary>
  AcpMode = 0x330,

  /// <summary>
  /// Р РµРіРёСЃС‚СЂ РґР°РЅРЅС‹С… РђР¦Рџ.
  /// </summary>
  AcpData = 0x332,

  /// <summary>
  /// Р РµРіРёСЃС‚СЂ РїРѕРґРєР»СЋС‡РµРЅРёСЏ С€РёРЅ РђР¦Рџ.
  /// </summary>
  AcpGate = 0x334,

  /// <summary>
  /// РџСЂРѕРіСЂР°РјРјРЅС‹Р№ СЂРµРіРёСЃС‚СЂ СЃС‚Р°СЂС‚Р° РђР¦Рџ.
  /// </summary>
  AcpStart = 0x338,

  /// <summary>
  /// РџСЂРѕРіСЂР°РјРјРЅС‹Р№ СЂРµРіРёСЃС‚СЂ СЃС‚РѕРїР° РђР¦Рџ.
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
public static class LegacyAskPpuMode
{
  /// <summary>
  /// Измерение в течение одной секунды.
  /// </summary>
  public const ushort OneSecond = 0x0001;

  /// <summary>
  /// Измерение в течение одной минуты.
  /// </summary>
  public const ushort OneMinute = 0x0002;

  /// <summary>
  /// Включение измерения напряжения ППУ.
  /// </summary>
  public const ushort MeasureVoltage = 0x0004;
}

/// <summary>
/// Формат адреса точки регистра <c>rgADDR</c>.
/// </summary>
public static class LegacyAskPointAddress
{
  /// <summary>
  /// Формирует адрес точки по номеру стойки, блоку коммутации и номеру точки.
  /// </summary>
  public static ushort Create(int chassis, int block, int point)
  {
    int safeChassis = Math.Clamp(chassis, 1, 8) - 1;
    int safeBlock = Math.Clamp(block, 1, 24) - 1;
    int safePoint = Math.Clamp(point, 0, 100);
    return (ushort)((safeChassis << 12) | (safeBlock << 7) | safePoint);
  }
}

/// <summary>
/// Сетевые адреса устройств старой АСК в протоколе MKI.
/// </summary>
public static class LegacyAskDeviceAddress
{
  /// <summary>
  /// Контроллер СКУ.
  /// </summary>
  public const byte Controller = 0x01;

  /// <summary>
  /// Сетевой блок ПКИ/ППУ.
  /// </summary>
  public const byte PpuPki = 0x02;
}

/// <summary>
/// Дополнительные регистры старой АСК, которые совпадают по номеру с базовыми регистрами и различаются сетевым адресом.
/// </summary>
public static class LegacyAskRegisters
{
  /// <summary>
  /// Слово режимов ПКИ/ППУ сетевого блока.
  /// </summary>
  public const LegacyAskRegister PpuNetMode = (LegacyAskRegister)0x302;

  /// <summary>
  /// Слово уставки ПКИ сетевого блока.
  /// </summary>
  public const LegacyAskRegister PpuNetLevel = (LegacyAskRegister)0x304;

  /// <summary>
  /// Слово напряжения ППУ сетевого блока.
  /// </summary>
  public const LegacyAskRegister PpuNetVoltage = (LegacyAskRegister)0x306;

  /// <summary>
  /// Слово команд ПКИ/ППУ сетевого блока.
  /// </summary>
  public const LegacyAskRegister PpuNetCommand = (LegacyAskRegister)0x308;

  /// <summary>
  /// Регистр режимов ППУ старого несетевого варианта.
  /// </summary>
  public const LegacyAskRegister PpuMkiMode = (LegacyAskRegister)0x32A;

  /// <summary>
  /// Регистр команд ППУ старого несетевого варианта.
  /// </summary>
  public const LegacyAskRegister PpuMkiCommand = (LegacyAskRegister)0x32C;
}

/// <summary>
/// Биты регистров ППУ старого несетевого варианта из <c>mkd_ppuo.c</c>.
/// </summary>
public static class LegacyAskPpuMkiBits
{
  /// <summary>
  /// Признак низковольтного диапазона ППУ.
  /// </summary>
  public const ushort LowRange = 0x2000;

  /// <summary>
  /// Признак средневольтного диапазона ППУ.
  /// </summary>
  public const ushort MiddleRange = 0x4000;

  /// <summary>
  /// Режим выдержки 1 секунда.
  /// </summary>
  public const ushort OneSecond = 0x0001;

  /// <summary>
  /// Режим выдержки 1 минута.
  /// </summary>
  public const ushort OneMinute = 0x0002;

  /// <summary>
  /// Подключение выхода ППУ к цифровому вольтметру.
  /// </summary>
  public const ushort MeasureVoltage = 0x0004;

  /// <summary>
  /// Включение индикатора ППУ.
  /// </summary>
  public const ushort Led = 0x0008;

  /// <summary>
  /// Признак годности, возвращаемый ППУ.
  /// </summary>
  public const ushort Good = 0x0080;

  /// <summary>
  /// Команда сброса ППУ.
  /// </summary>
  public const ushort Reset = 0x0100;

  /// <summary>
  /// Команда пуска ППУ.
  /// </summary>
  public const ushort Start = 0x0800;

  /// <summary>
  /// Признак сбоя ППУ.
  /// </summary>
  public const ushort Error = 0x2000;

  /// <summary>
  /// Признак занятости ППУ.
  /// </summary>
  public const ushort Busy = 0x8000;
}

/// <summary>
/// Биты регистров сетевого блока ПКИ/ППУ из <c>mkd_ppun.c</c>.
/// </summary>
public static class LegacyAskPpuNetBits
{
  /// <summary>
  /// Режим одной минуты в регистре режимов.
  /// </summary>
  public const ushort ModeOneMinute = 0x0800;

  /// <summary>
  /// Поле выбранного устройства в регистре режимов.
  /// </summary>
  public const ushort DeviceMask = 0x0700;

  /// <summary>
  /// Код подключения ППУ.
  /// </summary>
  public const ushort DevicePpu = 4;

  /// <summary>
  /// Код подключения ПКИ через СИ.
  /// </summary>
  public const ushort DevicePkiSi = 2;

  /// <summary>
  /// Поле диапазона напряжения ПКИ.
  /// </summary>
  public const ushort PkiVoltageRangeMask = 0x0070;

  /// <summary>
  /// Поле диапазона тока ПКИ.
  /// </summary>
  public const ushort PkiCurrentRangeMask = 0x000F;

  /// <summary>
  /// Режим одной секунды в регистре уставки.
  /// </summary>
  public const ushort LevelOneSecond = 0x8000;

  /// <summary>
  /// Индикация ПИ.
  /// </summary>
  public const ushort LevelPpu = 0x1000;

  /// <summary>
  /// Индикация СИ.
  /// </summary>
  public const ushort LevelPkiSi = 0x0800;

  /// <summary>
  /// Поле уставки ПКИ.
  /// </summary>
  public const ushort LevelMask = 0x03FF;

  /// <summary>
  /// Команда сброса ПКИ.
  /// </summary>
  public const ushort CommandPkiReset = 0x0100;

  /// <summary>
  /// Команда пуска ПКИ.
  /// </summary>
  public const ushort CommandPkiStart = 0x0200;

  /// <summary>
  /// Команда сброса ППУ.
  /// </summary>
  public const ushort CommandPpuReset = 0x0300;

  /// <summary>
  /// Команда пуска ППУ.
  /// </summary>
  public const ushort CommandPpuStart = 0x0400;

  /// <summary>
  /// Команда плавного спада ППУ.
  /// </summary>
  public const ushort CommandPpuSlowDischarge = 0x0500;

  /// <summary>
  /// Признак готовности ППУ.
  /// </summary>
  public const ushort PpuReady = 0x8000;

  /// <summary>
  /// Признак пробоя ППУ.
  /// </summary>
  public const ushort PpuBreakdown = 0x4000;

  /// <summary>
  /// Признак готовности ПКИ.
  /// </summary>
  public const ushort PkiReady = 0x2000;

  /// <summary>
  /// Признак результата ПКИ ниже уставки.
  /// </summary>
  public const ushort PkiLessThanLimit = 0x1000;
}

/// <summary>
/// Преобразование напряжения ППУ в старый код 2-4-2-1.
/// </summary>
public static class LegacyAskPpuVoltageCode
{
  private static readonly ushort[] DigitCodes = [0, 1, 2, 3, 4, 5, 6, 13, 14, 15];

  /// <summary>
  /// Кодирует целое напряжение в вольтах в формат 2-4-2-1, используемый регистрами ППУ.
  /// </summary>
  public static ushort FromVoltage(int voltage)
  {
    int safeVoltage = Math.Clamp(voltage, 0, 999);
    ushort result = 0;

    for (int digit = 0; digit < 3; digit++)
    {
      result |= (ushort)(DigitCodes[safeVoltage % 10] << (digit * 4));
      safeVoltage /= 10;
    }

    return result;
  }
}
