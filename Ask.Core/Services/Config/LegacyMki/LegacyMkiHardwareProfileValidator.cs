using System.Globalization;

namespace Ask.Core.Services.Config.LegacyMki;

/// <summary>
/// Проверяет legacy-конфигурацию аппаратуры АСК по тем же базовым ограничениям, которые старая MKI использует перед работой с оборудованием.
/// </summary>
public static class LegacyMkiHardwareProfileValidator
{
  private const byte VoltmeterAbsent = 9;
  private const byte BlockAbsent = 0;
  private const byte BlockBb1 = 1;
  private const byte PpuAbsent = 0;
  private const byte Ppu625 = 1;
  private const int MinPpuVoltage = 5;
  private const int MaxPpuRegisterVoltage = 700;
  private const int MaxPpuConfiguredVoltage = 625;
  private const int MinPiSeconds = 1;
  private const int MaxPiSeconds = 600;
  private const int MaxSwitchCount = 8;
  private const int MaxBkNumber = 24;
  private static readonly double[] AcpCurrentMaxValues = [0.010, 0.020, 0.050, 0.100];

  /// <summary>
  /// Проверяет профиль и возвращает список найденных ошибок.
  /// </summary>
  /// <param name="profile">Проверяемый профиль аппаратуры.</param>
  public static IReadOnlyList<LegacyMkiHardwareProfileValidationError> Validate(LegacyMkiHardwareProfile profile)
  {
    ArgumentNullException.ThrowIfNull(profile);

    var errors = new List<LegacyMkiHardwareProfileValidationError>();

    ValidateHardwareConfig(profile.HardwareConfig, errors);
    ValidateHardwareAux(profile, errors);
    ValidateTiming(profile, errors);
    ValidateDerivedRules(profile, errors);

    return errors;
  }

  /// <summary>
  /// Проверяет профиль и выбрасывает исключение, если найдены ошибки.
  /// </summary>
  /// <param name="profile">Проверяемый профиль аппаратуры.</param>
  public static void ThrowIfInvalid(LegacyMkiHardwareProfile profile)
  {
    var errors = Validate(profile);
    if (errors.Count > 0)
    {
      throw new LegacyMkiHardwareProfileValidationException(errors);
    }
  }

  /// <summary>
  /// Проверяет основной блок аппаратной конфигурации.
  /// </summary>
  private static void ValidateHardwareConfig(
    LegacyMkiHardwareConfigSection hardware,
    List<LegacyMkiHardwareProfileValidationError> errors)
  {
    ValidateOption(hardware.DvAcp, 0, 2, "HardwareConfig.DvAcp", "Наличие АЦП", errors);
    ValidateOption(hardware.DvV7, 0, 9, "HardwareConfig.DvV7", "Тип цифрового вольтметра", errors);
    ValidateOption(hardware.EtGui4, 0, 1, "HardwareConfig.EtGui4", "Источник тока для ЭТ", errors);
    ValidateOption(hardware.KuGui4, 0, 1, "HardwareConfig.KuGui4", "Команды КУ/СИ с ПИНТ4", errors);
    ValidateOption(hardware.IsRos, 0, 1, "HardwareConfig.IsRos", "Схема НК", errors);
    ValidateOption(hardware.TyPpu, PpuAbsent, Ppu625, "HardwareConfig.TyPpu", "ППУ", errors);
    ValidateOption(hardware.AcpTmr, 0, 1, "HardwareConfig.AcpTmr", "КИ с контроллером", errors);
    ValidateOption(hardware.NAcpMaMax, 0, 3, "HardwareConfig.NAcpMaMax", "Imax АЦП", errors);
    ValidateOption(hardware.IsPki, 0, 1, "HardwareConfig.IsPki", "ПКИ", errors);
    ValidateOption(hardware.Comx4Com1, 0, 8, "HardwareConfig.Comx4Com1", "COM-порт COM-разветвителя", errors);
    ValidateOption(hardware.BbSpr, 0, 2, "HardwareConfig.BbSpr", "Блок блокировок", errors);
    ValidateOption(hardware.LcIs, 0, 1, "HardwareConfig.LcIs", "Измерение емкости", errors);
    ValidateOption(hardware.PkiExtMo, 0, 1, "HardwareConfig.PkiExtMo", "Режим выбора диапазона тока ПКИ", errors);
    ValidateOption(hardware.AcpIs0_3V, 0, 1, "HardwareConfig.AcpIs0_3V", "Диапазон U АЦП 0.3 В", errors);
    ValidateOption(hardware.DivGatBk, 0, 1, "HardwareConfig.DivGatBk", "Разделенные входы БК", errors);
    ValidateOption(hardware.EkFull, 0, 1, "HardwareConfig.EkFull", "Полнодоступный ЭК", errors);
    ValidateOption(hardware.CalcPgr, 0, 1, "HardwareConfig.CalcPgr", "Расчет погрешностей", errors);

    ValidateArrayLength(hardware.SkIs, MaxSwitchCount, "HardwareConfig.SkIs", "Наличие СК", errors);
    ValidateArrayLength(hardware.SkBkBeg, MaxSwitchCount, "HardwareConfig.SkBkBeg", "Первый БК", errors);
    ValidateArrayLength(hardware.SkBkEnd, MaxSwitchCount, "HardwareConfig.SkBkEnd", "Последний БК", errors);
    ValidateArrayLength(hardware.GuiType, 2, "HardwareConfig.GuiType", "Наличие и тип ПИНТ", errors);
    ValidateArrayLength(hardware.GuiVoltStep, 2, "HardwareConfig.GuiVoltStep", "Шаг напряжения ПИНТ", errors);
    ValidateArrayLength(hardware.GuiAmperStep, 2, "HardwareConfig.GuiAmperStep", "Шаг тока ПИНТ", errors);
    ValidateArrayLength(hardware.GuiVoltMax, 2, "HardwareConfig.GuiVoltMax", "Максимальное напряжение ПИНТ", errors);
    ValidateArrayLength(hardware.GuiAmperMax, 2, "HardwareConfig.GuiAmperMax", "Максимальный ток ПИНТ", errors);

    ValidateSwitchRanges(hardware, errors);
    ValidatePintRanges(hardware, errors);

    ValidateNonNegativeFinite(hardware.GomCmt, "HardwareConfig.GomCmt", "R изоляции коммутатора", errors);
    ValidateNonNegativeFinite(hardware.RbusBb, "HardwareConfig.RbusBb", "Rдобавочное шины", errors);
    ValidateNonNegativeFinite(hardware.UmaxEk, "HardwareConfig.UmaxEk", "Umax на шинах ЭК", errors);
    ValidateIntegerRange(hardware.UmaxSiEkFull, 0, MaxPpuConfiguredVoltage, "HardwareConfig.UmaxSiEkFull", "Umax СИ полнодоступного ЭК", errors);
    ValidateIntegerRange(hardware.UmaxPiEkFull, 0, MaxPpuConfiguredVoltage, "HardwareConfig.UmaxPiEkFull", "Umax ПИ полнодоступного ЭК", errors);
  }

  /// <summary>
  /// Проверяет дополнительные аппаратные параметры.
  /// </summary>
  private static void ValidateHardwareAux(
    LegacyMkiHardwareProfile profile,
    List<LegacyMkiHardwareProfileValidationError> errors)
  {
    var aux = profile.HardwareAux;

    ValidateOption(aux.Res1, 0, 1, "HardwareAux.Res1", "Резервный флаг", errors);
    ValidateOption(aux.IsTstUpki, 0, 1, "HardwareAux.IsTstUpki", "Аппаратный контроль Uпки", errors);
    ValidateOption(aux.Net, 0, 1, "HardwareAux.Net", "Сетевой протокол", errors);
    ValidateOption(aux.BeepOff, 0, 1, "HardwareAux.BeepOff", "Отключать звук", errors);
    ValidateOption(aux.Meas2, 0, 1, "HardwareAux.Meas2", "Отбрасывать первый замер", errors);
    ValidateOption(aux.LocErrSob, 0, 1, "HardwareAux.LocErrSob", "Локализация ошибки обмена", errors);
    ValidateOption(aux.ShortSsRt, 0, 1, "HardwareAux.ShortSsRt", "Короткое замыкание СС/РТ", errors);
    ValidateOption(aux.UseWait, 0, 2, "HardwareAux.UseWait", "Алгоритмы ускорения", errors);
    ValidateOption(aux.ReioVm, 0, 1, "HardwareAux.ReioVm", "Повторная инициализация вольтметра", errors);
    ValidateOption(aux.OutUpi, 0, 1, "HardwareAux.OutUpi", "Выводить Uппу в протокол", errors);
    ValidateOption(aux.ReioGui3, 0, 1, "HardwareAux.ReioGui3", "Повторная инициализация ПИНТ3", errors);

    ValidateArrayLength(aux.PkiAkomDiv, 8, "HardwareAux.PkiAkomDiv", "R нижнего плеча делителя ПКИ", errors);
    ValidateArrayLength(aux.PkiKomTst, 10, "HardwareAux.PkiKomTst", "Сопротивление НР-4", errors);
    ValidateArrayLength(aux.PkiAVolt, 5, "HardwareAux.PkiAVolt", "Входное напряжение ПКИ", errors);

    ValidatePkiAux(profile, errors);
    ValidatePort(aux.PortSku, "HardwareAux.PortSku", "СКУ", isRequired: true, errors);
    ValidatePort(aux.PortVm, "HardwareAux.PortVm", "Вольтметр", RequiresVoltmeterCom(profile.HardwareConfig.DvV7), errors);
    ValidatePort(aux.PortGui3, "HardwareAux.PortGui3", "ПИНТ3", IsPintEnabled(profile.HardwareConfig, 3), errors);

    ValidateIntegerRange(aux.U220 == 0 ? 220 : aux.U220, 6, 260, "HardwareAux.U220", "Фазное напряжение сети", errors);
    ValidateNonNegativeFinite(aux.RwirAdc, "HardwareAux.RwirAdc", "R короткозамкнутого входа АЦП", errors);
    ValidatePositiveFinite(aux.PpuKmul, "HardwareAux.PpuKmul", "Коэффициент делителя ППУ", errors);
    ValidateNonNegativeFinite(aux.UacpR, "HardwareAux.UacpR", "U АЦП для теста", errors);
    ValidateNonNegativeFinite(aux.Uv7R, "HardwareAux.Uv7R", "U вольтметра для теста АЦП", errors);
    ValidateNonNegativeFinite(aux.RwirV7, "HardwareAux.RwirV7", "R короткозамкнутого входа вольтметра", errors);
    ValidateNonNegativeFinite(aux.Rgui4, "HardwareAux.Rgui4", "R активной нагрузки ПИНТ4", errors);
    ValidateNonNegativeFinite(aux.DIGui4mA, "HardwareAux.DIGui4mA", "dI активной нагрузки ПИНТ4", errors);
    ValidateNonNegativeFinite(aux.KmulKi, "HardwareAux.KmulKi", "K умножения измеренного времени", errors);
    ValidateNonNegativeFinite(aux.TdobTdo, "HardwareAux.TdobTdo", "tдо_доб", errors);
    ValidateNonNegativeFinite(aux.TdobTi, "HardwareAux.TdobTi", "tи_доб", errors);
    ValidateIntegerRange(aux.KopAddr, 0, 30, "HardwareAux.KopAddr", "Адрес вольтметра на КОП", errors);
    ValidateIntegerRange(aux.QMeasC, 0, 100, "HardwareAux.QMeasC", "Количество замеров емкости", errors);
    ValidateIntegerRange(aux.MksAcpTmr, 0, 1_000_000, "HardwareAux.MksAcpTmr", "Время измерения контроллером", errors);
  }

  /// <summary>
  /// Проверяет временные параметры аппаратуры.
  /// </summary>
  private static void ValidateTiming(
    LegacyMkiHardwareProfile profile,
    List<LegacyMkiHardwareProfileValidationError> errors)
  {
    var timing = profile.Timing;
    ValidateIntegerRange(timing.SkPwr, 0, ushort.MaxValue, "Timing.SkPwr", "Включение питания СК", errors);
    ValidateIntegerRange(timing.BkBus, 0, ushort.MaxValue, "Timing.BkBus", "Коммутация БК к ИШ", errors);
    ValidateIntegerRange(timing.EkRk, 0, ushort.MaxValue, "Timing.EkRk", "Коммутация групповых реле", errors);
    ValidateIntegerRange(timing.PtEk, 0, ushort.MaxValue, "Timing.PtEk", "Коммутация точек к шинам ЭК", errors);
    ValidateIntegerRange(timing.PtRk, 0, ushort.MaxValue, "Timing.PtRk", "Коммутация точек к шинам РК", errors);
    ValidateIntegerRange(timing.EpPwr, 0, ushort.MaxValue, "Timing.EpPwr", "Коммутация КЭП к шинам ЭК", errors);
    ValidateIntegerRange(timing.KzSh, 0, ushort.MaxValue, "Timing.KzSh", "Коммутация реле КЗШ", errors);
    ValidateIntegerRange(timing.GuiPwr, 0, ushort.MaxValue, "Timing.GuiPwr", "Задержка включения питания ПИНТ", errors);
    ValidateIntegerRange(timing.Gui4Mod, 0, ushort.MaxValue, "Timing.Gui4Mod", "Задержка установки режима ПИНТ4", errors);
    ValidateIntegerRange(timing.Gui3Mod, 0, ushort.MaxValue, "Timing.Gui3Mod", "Задержка установки режима ПИНТ3", errors);
    ValidateIntegerRange(timing.GuiRst, 0, ushort.MaxValue, "Timing.GuiRst", "Задержка восстановления режима ПИНТ", errors);
    ValidateIntegerRange(timing.GuiGat, 0, ushort.MaxValue, "Timing.GuiGat", "Задержка коммутации ПИНТ к шинам", errors);
    ValidateIntegerRange(timing.V734Mod, 0, ushort.MaxValue, "Timing.V734Mod", "В7-34: установка режима", errors);
    ValidateIntegerRange(timing.V753Mod, 0, ushort.MaxValue, "Timing.V753Mod", "В7-53: установка режима", errors);
    ValidateIntegerRange(timing.V765Mod, 0, ushort.MaxValue, "Timing.V765Mod", "В7-65/72/73/87: установка режима", errors);
    ValidateIntegerRange(timing.V7Gat, 0, ushort.MaxValue, "Timing.V7Gat", "Задержка коммутации вольтметра", errors);
    ValidateIntegerRange(timing.AcpMod, 0, ushort.MaxValue, "Timing.AcpMod", "Задержка установки режима АЦП", errors);
    ValidateIntegerRange(timing.AcpGat, 0, ushort.MaxValue, "Timing.AcpGat", "Задержка коммутации АЦП", errors);
    ValidateIntegerRange(timing.PkiPwr, 0, ushort.MaxValue, "Timing.PkiPwr", "Задержка включения питания ПКИ", errors);
    ValidateIntegerRange(timing.PkiMod, 0, ushort.MaxValue, "Timing.PkiMod", "Задержка установки режима ПКИ", errors);
    ValidateIntegerRange(timing.PpuPwr, 0, ushort.MaxValue, "Timing.PpuPwr", "Задержка включения питания ППУ", errors);
    ValidateIntegerRange(timing.PpuMod, 0, ushort.MaxValue, "Timing.PpuMod", "Задержка смены режима ППУ", errors);
    ValidateIntegerRange(timing.KoPwr, 0, ushort.MaxValue, "Timing.KoPwr", "Задержка между записями в КОП", errors);
    ValidateIntegerRange(timing.EpBef, 0, ushort.MaxValue, "Timing.EpBef", "Задержка перед измерением КЭП", errors);
    ValidateIntegerRange(timing.V7Bef, 0, ushort.MaxValue, "Timing.V7Bef", "Задержка перед измерением вольтметром", errors);
    ValidateIntegerRange(timing.AcpBef, 0, ushort.MaxValue, "Timing.AcpBef", "Задержка перед измерением АЦП", errors);
    ValidateIntegerRange(timing.PkiBef, 0, ushort.MaxValue, "Timing.PkiBef", "Задержка перед измерением ПКИ", errors);
    ValidateIntegerRange(timing.PpuBef, 0, ushort.MaxValue, "Timing.PpuBef", "Задержка перед измерением ППУ", errors);
    ValidateIntegerRange(timing.PpuAftPusk, 0, ushort.MaxValue, "Timing.PpuAftPusk", "Задержка после пуска ППУ", errors);
    ValidateIntegerRange(timing.TMeasUppuMin, 0, ushort.MaxValue, "Timing.TMeasUppuMin", "Минимальное время измерения Uппу", errors);
    ValidateIntegerRange(timing.LcBef, 0, ushort.MaxValue, "Timing.LcBef", "Задержка перед измерением C", errors);
  }

  /// <summary>
  /// Проверяет связи параметров, которые в старой программе вычисляются через CFG-функции.
  /// </summary>
  private static void ValidateDerivedRules(
    LegacyMkiHardwareProfile profile,
    List<LegacyMkiHardwareProfileValidationError> errors)
  {
    var hardware = profile.HardwareConfig;

    if (hardware.BbSpr == BlockBb1 && hardware.TyPpu != PpuAbsent)
    {
      Add(errors, "HardwareConfig.TyPpu", "При блоке блокировок ББ1 ППУ считается отсутствующим. Отключите ППУ или измените блок блокировок.");
    }

    if (hardware.TyPpu != PpuAbsent && hardware.TyPpu != Ppu625)
    {
      Add(errors, "HardwareConfig.TyPpu", "Для ППУ поддерживается только режим \"имеется, 625 В\".");
    }

    if (hardware.TyPpu == Ppu625)
    {
      ValidatePiVoltage(MaxPpuConfiguredVoltage, false, "HardwareConfig.TyPpu", errors);
    }

    if (hardware.IsPki != 0)
    {
      ValidatePiVoltage(hardware.PkiUmax, true, "HardwareConfig.PkiUmax", errors);
    }

    if (hardware.AcpTmr != 0 && profile.HardwareAux.Net == 0)
    {
      Add(errors, "HardwareConfig.AcpTmr", "КИ с контроллером требует включенного сетевого протокола с контроллером.");
    }

    if (hardware.EkFull != 0 && hardware.UmaxSiEkFull == 0 && hardware.UmaxPiEkFull == 0)
    {
      Add(errors, "HardwareConfig.EkFull", "Для полнодоступного ЭК задайте Umax СИ или Umax ПИ.");
    }
  }

  /// <summary>
  /// Проверяет диапазоны СК/БК.
  /// </summary>
  private static void ValidateSwitchRanges(
    LegacyMkiHardwareConfigSection hardware,
    List<LegacyMkiHardwareProfileValidationError> errors)
  {
    for (var index = 0; index < MaxSwitchCount; index++)
    {
      var switchNumber = index + 1;
      var exists = index == 0 || hardware.SkIs.ElementAtOrDefault(index) != 0;

      ValidateOption(hardware.SkIs.ElementAtOrDefault(index), 0, 1, $"HardwareConfig.SkIs[{index}]", $"СК-{switchNumber}: имеется", errors);
      if (!exists)
      {
        continue;
      }

      var firstBk = hardware.SkBkBeg.ElementAtOrDefault(index);
      var lastBk = hardware.SkBkEnd.ElementAtOrDefault(index);

      ValidateIntegerRange(firstBk, 1, MaxBkNumber, $"HardwareConfig.SkBkBeg[{index}]", $"СК-{switchNumber}: первый БК", errors);
      ValidateIntegerRange(lastBk, 1, MaxBkNumber, $"HardwareConfig.SkBkEnd[{index}]", $"СК-{switchNumber}: последний БК", errors);

      if (firstBk > lastBk)
      {
        Add(errors, $"HardwareConfig.SkBkBeg[{index}]", $"СК-{switchNumber}: первый БК не может быть больше последнего БК.");
      }
    }
  }

  /// <summary>
  /// Проверяет параметры ПИНТ3 и ПИНТ4.
  /// </summary>
  private static void ValidatePintRanges(
    LegacyMkiHardwareConfigSection hardware,
    List<LegacyMkiHardwareProfileValidationError> errors)
  {
    for (var index = 0; index < 2; index++)
    {
      var pint = index + 3;
      var type = hardware.GuiType.ElementAtOrDefault(index);
      var maxType = index == 0 ? 2 : 2;
      ValidateOption(type, 0, maxType, $"HardwareConfig.GuiType[{index}]", $"ПИНТ{pint}: тип", errors);

      if (type == 0)
      {
        continue;
      }

      ValidateStepAndMax(hardware.GuiVoltStep.ElementAtOrDefault(index), hardware.GuiVoltMax.ElementAtOrDefault(index), $"HardwareConfig.GuiVoltStep[{index}]", $"HardwareConfig.GuiVoltMax[{index}]", $"ПИНТ{pint}: напряжение", errors);
      ValidateStepAndMax(hardware.GuiAmperStep.ElementAtOrDefault(index), hardware.GuiAmperMax.ElementAtOrDefault(index), $"HardwareConfig.GuiAmperStep[{index}]", $"HardwareConfig.GuiAmperMax[{index}]", $"ПИНТ{pint}: ток", errors);
    }
  }

  /// <summary>
  /// Проверяет параметры ПКИ, включая таблицы напряжений и сопротивлений.
  /// </summary>
  private static void ValidatePkiAux(
    LegacyMkiHardwareProfile profile,
    List<LegacyMkiHardwareProfileValidationError> errors)
  {
    var hardware = profile.HardwareConfig;
    var aux = profile.HardwareAux;

    if (hardware.IsPki == 0)
    {
      return;
    }

    ValidateIntegerRange(hardware.PkiUmax, MinPpuVoltage, 500, "HardwareConfig.PkiUmax", "Umax ПКИ", errors);

    for (var index = 0; index < aux.PkiAVolt.Length; index++)
    {
      var value = aux.PkiAVolt[index];
      ValidatePositiveFinite(value, $"HardwareAux.PkiAVolt[{index}]", $"Входное напряжение ПКИ диапазона {index + 1}", errors);
      if (value > hardware.PkiUmax)
      {
        Add(errors, $"HardwareAux.PkiAVolt[{index}]", $"Входное напряжение ПКИ диапазона {index + 1} не может быть больше Umax ПКИ.");
      }
    }

    for (var index = 0; index < aux.PkiAkomDiv.Length; index++)
    {
      var value = aux.PkiAkomDiv[index];
      if (index < 5 || IsPkiCurrentRangeBinary(profile))
      {
        ValidatePositiveFinite(value, $"HardwareAux.PkiAkomDiv[{index}]", $"R нижнего плеча делителя ПКИ диапазона {index + 1}", errors);
      }
    }

    for (var index = 0; index < aux.PkiKomTst.Length; index++)
    {
      ValidatePositiveFinite(aux.PkiKomTst[index], $"HardwareAux.PkiKomTst[{index}]", $"Сопротивление НР-4 #{index + 1}", errors);
    }
  }

  /// <summary>
  /// Проверяет настройки COM-порта.
  /// </summary>
  private static void ValidatePort(
    LegacyMkiPortSettings port,
    string path,
    string title,
    bool isRequired,
    List<LegacyMkiHardwareProfileValidationError> errors)
  {
    if (!isRequired && IsEmptyPort(port))
    {
      return;
    }

    ValidateOption(port.Com1, 0, 12, $"{path}.Com1", $"{title}: COM-порт или канал", errors);
    ValidateOption(port.Baud, 0, 7, $"{path}.Baud", $"{title}: скорость обмена", errors);
    ValidateOption(port.Parity, 0, 4, $"{path}.Parity", $"{title}: паритет", errors);
    ValidateIntegerRange(port.Len, 5, 8, $"{path}.Len", $"{title}: длина посылки", errors);
    ValidateIntegerRange(port.QStopBit, 1, 2, $"{path}.QStopBit", $"{title}: количество стоп-бит", errors);
    ValidateIntegerRange(port.MsTmo, isRequired ? 1 : 0, ushort.MaxValue, $"{path}.MsTmo", $"{title}: тайм-аут обмена", errors);
    ValidateIntegerRange(port.MksWait, 0, ushort.MaxValue, $"{path}.MksWait", $"{title}: COM-задержка", errors);
  }

  /// <summary>
  /// Проверяет, требуется ли COM-порт выбранному типу цифрового вольтметра.
  /// </summary>
  private static bool RequiresVoltmeterCom(byte voltmeterType)
  {
    return voltmeterType is 5 or 6 or 8;
  }

  /// <summary>
  /// Проверяет, что COM-блок не заполнен и может считаться отключенным.
  /// </summary>
  private static bool IsEmptyPort(LegacyMkiPortSettings port)
  {
    return port.Com1 == 0
      && port.Baud == 0
      && port.Parity == 0
      && port.Protocol == 0
      && port.QStopBit == 0
      && port.RtsDtr == 0
      && port.MsTmo == 0
      && port.MksWait == 0
      && port.Len == 0
      && port.Base == 0
      && port.Reserved.All(value => value == 0);
  }

  /// <summary>
  /// Проверяет пару "шаг/максимум" для регулируемого источника.
  /// </summary>
  private static void ValidateStepAndMax(
    double step,
    double max,
    string stepPath,
    string maxPath,
    string title,
    List<LegacyMkiHardwareProfileValidationError> errors)
  {
    ValidatePositiveFinite(step, stepPath, $"{title}: шаг", errors);
    ValidatePositiveFinite(max, maxPath, $"{title}: максимум", errors);

    if (IsFinite(step) && IsFinite(max) && step > 0 && max > 0 && step > max)
    {
      Add(errors, stepPath, $"{title}: шаг не может быть больше максимального значения.");
    }
  }

  /// <summary>
  /// Проверяет ограничения напряжения и времени для ПКИ/ППУ из функции PIchkUT старой MKI.
  /// </summary>
  private static void ValidatePiVoltage(double voltage, bool isPki, string path, List<LegacyMkiHardwareProfileValidationError> errors)
  {
    if (Math.Truncate(voltage) != voltage)
    {
      Add(errors, path, $"{(isPki ? "ПКИ" : "ППУ")}: U должно быть целым.");
    }

    if (isPki)
    {
      if (voltage <= 0)
      {
        Add(errors, path, "ПКИ: не задано U.");
      }

      return;
    }

    if (voltage < MinPpuVoltage)
    {
      Add(errors, path, $"ППУ: U меньше Umin ({MinPpuVoltage} В).");
    }

    if (voltage > MaxPpuRegisterVoltage)
    {
      Add(errors, path, $"ППУ: U больше Umax ({MaxPpuRegisterVoltage} В).");
    }
  }

  /// <summary>
  /// Проверяет время выдержки для испытаний из функции PIchkUT старой MKI.
  /// </summary>
  public static void ValidatePiTimeSeconds(double seconds, string path)
  {
    var errors = new List<LegacyMkiHardwareProfileValidationError>();
    if (Math.Truncate(seconds) != seconds)
    {
      Add(errors, path, "t должно быть целым.");
    }

    if (seconds < MinPiSeconds || seconds > MaxPiSeconds)
    {
      Add(errors, path, "t должно быть в интервале от 1 с до 600 с.");
    }

    if (errors.Count > 0)
    {
      throw new LegacyMkiHardwareProfileValidationException(errors);
    }
  }

  /// <summary>
  /// Возвращает признак двоичного выбора диапазонов тока ПКИ.
  /// </summary>
  private static bool IsPkiCurrentRangeBinary(LegacyMkiHardwareProfile profile)
  {
    return profile.HardwareAux.Net != 0
      || profile.HardwareAux.PkiAkomDiv.ElementAtOrDefault(5) > 0;
  }

  /// <summary>
  /// Проверяет, включен ли ПИНТ с указанным номером.
  /// </summary>
  private static bool IsPintEnabled(LegacyMkiHardwareConfigSection hardware, int pint)
  {
    var index = pint - 3;
    return index >= 0
      && index < hardware.GuiType.Length
      && hardware.GuiType[index] != 0;
  }

  /// <summary>
  /// Проверяет значение с ограниченным набором вариантов.
  /// </summary>
  private static void ValidateOption(byte value, int min, int max, string path, string title, List<LegacyMkiHardwareProfileValidationError> errors)
  {
    ValidateIntegerRange(value, min, max, path, title, errors);
  }

  /// <summary>
  /// Проверяет целочисленный диапазон.
  /// </summary>
  private static void ValidateIntegerRange(double value, double min, double max, string path, string title, List<LegacyMkiHardwareProfileValidationError> errors)
  {
    if (value < min || value > max)
    {
      Add(errors, path, $"{title}: значение {Format(value)} вне диапазона {Format(min)}...{Format(max)}.");
    }
  }

  /// <summary>
  /// Проверяет, что число конечно и не меньше нуля.
  /// </summary>
  private static void ValidateNonNegativeFinite(double value, string path, string title, List<LegacyMkiHardwareProfileValidationError> errors)
  {
    if (!IsFinite(value) || value < 0)
    {
      Add(errors, path, $"{title}: значение должно быть неотрицательным числом.");
    }
  }

  /// <summary>
  /// Проверяет, что число конечно и больше нуля.
  /// </summary>
  private static void ValidatePositiveFinite(double value, string path, string title, List<LegacyMkiHardwareProfileValidationError> errors)
  {
    if (!IsFinite(value) || value <= 0)
    {
      Add(errors, path, $"{title}: значение должно быть больше нуля.");
    }
  }

  /// <summary>
  /// Проверяет длину массива параметров.
  /// </summary>
  private static void ValidateArrayLength<T>(T[] values, int expectedLength, string path, string title, List<LegacyMkiHardwareProfileValidationError> errors)
  {
    if (values.Length != expectedLength)
    {
      Add(errors, path, $"{title}: ожидается {expectedLength} значений, фактически {values.Length}.");
    }
  }

  /// <summary>
  /// Проверяет, что число может использоваться в расчетах.
  /// </summary>
  private static bool IsFinite(double value)
  {
    return !double.IsNaN(value) && !double.IsInfinity(value);
  }

  /// <summary>
  /// Форматирует число для сообщения об ошибке.
  /// </summary>
  private static string Format(double value)
  {
    return value.ToString("0.###", CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Добавляет ошибку в список.
  /// </summary>
  private static void Add(List<LegacyMkiHardwareProfileValidationError> errors, string path, string message)
  {
    errors.Add(new LegacyMkiHardwareProfileValidationError(path, message));
  }
}
