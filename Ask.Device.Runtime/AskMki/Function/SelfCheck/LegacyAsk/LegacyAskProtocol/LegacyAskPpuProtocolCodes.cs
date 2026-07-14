namespace Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

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
