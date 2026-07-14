using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Самоконтроль цифрового вольтметра старого тестера АСК.
/// </summary>

/// <summary>
/// Общие функции форматирования и генерации точек старого самоконтроля АСК.
/// </summary>
/// <summary>
/// Выполняет обмен с ППУ и ПКИ по регистрам старой АСК.
/// </summary>
internal static class LegacyAskPpuPkiExchange
{
  /// <summary>
  /// Создает отдельный протокол для сетевого блока ПКИ/ППУ или возвращает <c>null</c> для несетевой конфигурации.
  /// </summary>
  public static LegacyAskControllerProtocol? CreatePpuPkiController(Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile, bool isIdleMode)
  {
    return profile.HardwareAux.Net == 0
      ? null
      : new LegacyAskControllerProtocol(LegacyAskControllerProtocol.CreateOptions(profile, isIdleMode, LegacyAskDeviceAddress.PpuPki));
  }

  /// <summary>
  /// Устанавливает напряжение и режим ППУ теми же регистрами, которые использует старая MKI.
  /// </summary>
  public static async Task SetPpuModeAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, int voltage, ushort mode)
  {
    int safeVoltage = Math.Clamp(voltage, 0, 999);
    ushort voltageCode = LegacyAskPpuVoltageCode.FromVoltage(safeVoltage);

    if (context.Profile.HardwareAux.Net != 0)
    {
      ushort modeWord = (ushort)(LegacyAskPpuNetBits.DevicePpu << 8);
      if ((mode & LegacyAskPpuMode.OneMinute) != 0)
      {
        modeWord |= LegacyAskPpuNetBits.ModeOneMinute;
      }

      ushort levelWord = LegacyAskPpuNetBits.LevelPpu;
      if ((mode & LegacyAskPpuMode.OneSecond) != 0)
      {
        levelWord |= LegacyAskPpuNetBits.LevelOneSecond;
      }

      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetVoltage, voltageCode, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetMode, modeWord, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetLevel, levelWord, context.CancellationToken);
      return;
    }

    ushort range = safeVoltage <= 125 ? LegacyAskPpuMkiBits.LowRange : LegacyAskPpuMkiBits.MiddleRange;
    ushort modeWordMki = (ushort)(voltageCode | range);
    ushort commandWord = ToMkiPpuMode(mode);

    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiMode, modeWordMki, context.CancellationToken);
    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiCommand, commandWord, context.CancellationToken);
  }

  /// <summary>
  /// Запускает ППУ после установки режима.
  /// </summary>
  public static Task StartPpuAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, ushort mode)
  {
    return context.Profile.HardwareAux.Net != 0
      ? controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetCommand, LegacyAskPpuNetBits.CommandPpuStart, context.CancellationToken)
      : controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiCommand, (ushort)(ToMkiPpuMode(mode) | LegacyAskPpuMkiBits.Led | LegacyAskPpuMkiBits.Start), context.CancellationToken);
  }

  /// <summary>
  /// Читает статус ППУ и преобразует аппаратные признаки сбоя в понятную ошибку теста.
  /// </summary>
  public static async Task ReadPpuStatusAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller)
  {
    ushort status = context.Profile.HardwareAux.Net != 0
      ? await controller.ReadRegisterAsync(LegacyAskRegisters.PpuNetCommand, context.CancellationToken)
      : await controller.ReadRegisterAsync(LegacyAskRegisters.PpuMkiCommand, context.CancellationToken);

    if (context.Profile.HardwareAux.Net != 0 && (status & LegacyAskPpuNetBits.PpuBreakdown) != 0)
    {
      throw new LegacyAskProtocolException("ППУ сообщила пробой.");
    }

    if (context.Profile.HardwareAux.Net != 0 && (status & LegacyAskPpuNetBits.PpuReady) == 0 && !ExecutionConfig.GetIsIdleModeEnabled())
    {
      throw new LegacyAskProtocolException("ППУ не вернула признак готовности.");
    }

    if (context.Profile.HardwareAux.Net == 0 && (status & LegacyAskPpuMkiBits.Error) != 0)
    {
      throw new LegacyAskProtocolException("ППУ сообщила сбой.");
    }

    if (context.Profile.HardwareAux.Net == 0 && (status & LegacyAskPpuMkiBits.Busy) != 0 && !ExecutionConfig.GetIsIdleModeEnabled())
    {
      throw new LegacyAskProtocolException("ППУ не завершила выполнение режима.");
    }
  }

  /// <summary>
  /// Сбрасывает ППУ после проверки.
  /// </summary>
  public static async Task ResetPpuAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller)
  {
    if (context.Profile.HardwareAux.Net != 0)
    {
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetCommand, LegacyAskPpuNetBits.CommandPpuReset, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetVoltage, 0, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetMode, 0, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetLevel, 0, context.CancellationToken);
      return;
    }

    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiCommand, LegacyAskPpuMkiBits.Reset, context.CancellationToken);
    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiMode, 0, context.CancellationToken);
  }

  /// <summary>
  /// Выполняет один цикл измерения ПКИ через сетевой блок или через базовый контроллер старой АСК.
  /// </summary>
  public static async Task RunPkiMeasurementAsync(LegacyAskSelfControlContext context, LegacyAskControllerProtocol controller, int voltageRange, double resistanceOhm)
  {
    if (context.Profile.HardwareAux.Net != 0)
    {
      int dU = Math.Clamp(voltageRange, 1, 7);
      int nlev = Math.Clamp((int)Math.Round(resistanceOhm / 1_000_000.0), 1, LegacyAskPpuNetBits.LevelMask);
      ushort modeWord = (ushort)((LegacyAskPpuNetBits.DevicePkiSi << 8) | (dU << 4) | 1);
      ushort levelWord = (ushort)(LegacyAskPpuNetBits.LevelPkiSi | (nlev ^ LegacyAskPpuNetBits.LevelMask));

      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetMode, modeWord, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetLevel, levelWord, context.CancellationToken);
      await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetCommand, LegacyAskPpuNetBits.CommandPkiStart, context.CancellationToken);
      ushort status = await controller.ReadRegisterAsync(LegacyAskRegisters.PpuNetCommand, context.CancellationToken);

      if ((status & LegacyAskPpuNetBits.PkiReady) == 0 && !ExecutionConfig.GetIsIdleModeEnabled())
      {
        throw new LegacyAskProtocolException("ПКИ не вернула признак готовности.");
      }

      return;
    }

    ushort commandWord = (ushort)(LegacyAskCommandBits.ElectronicProbe | LegacyAskCommandBits.ElectronicTop | LegacyAskCommandBits.ElectronicBottom);
    await controller.WriteCommandRegisterAsync(commandWord, context.CancellationToken);
    await controller.ReadAdcAsync(context.CancellationToken);
  }

  /// <summary>
  /// Преобразует абстрактный режим ППУ в слово команд несетевого регистра ППУ MKI.
  /// </summary>
  private static ushort ToMkiPpuMode(ushort mode)
  {
    ushort result = 0;
    if ((mode & LegacyAskPpuMode.OneMinute) != 0)
    {
      result |= LegacyAskPpuMkiBits.OneMinute;
    }
    else if ((mode & LegacyAskPpuMode.OneSecond) != 0)
    {
      result |= LegacyAskPpuMkiBits.OneSecond;
    }

    if ((mode & LegacyAskPpuMode.MeasureVoltage) != 0)
    {
      result |= LegacyAskPpuMkiBits.MeasureVoltage;
    }

    return result;
  }
}

