using Ask.Core.Services.Config.LegacyMki;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Device.Runtime.Device.ASKMKI_ACP;
using Ask.Device.Runtime.Device.Breakdowntester;
using Ask.Device.Runtime.Device.PINT;
using Ask.Device.Runtime.Device.PKI;
using Ask.Device.Runtime.Device.RelaySwitchModule;
using Ask.Device.Runtime.Device.TIMER;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Runtime-набор устройств старого тестера АСК для одного запуска самоконтроля.
/// </summary>
public sealed class LegacyAskDeviceComposition
{
  private LegacyAskDeviceComposition(
    IAskMkiController controller,
    IAskMkiAcp acp,
    IAskMkiCommutator commutator,
    IAskMkiTimer timer,
    IReadOnlyDictionary<int, IAskMkiPint> pints,
    IAskMkiPpu? ppu,
    IAskMkiPki? pki)
  {
    Controller = controller;
    Acp = acp;
    Commutator = commutator;
    Timer = timer;
    Pints = pints;
    Ppu = ppu;
    Pki = pki;
  }

  public IAskMkiController Controller { get; }

  public IAskMkiAcp Acp { get; }

  public IAskMkiCommutator Commutator { get; }

  public IAskMkiTimer Timer { get; }

  public IReadOnlyDictionary<int, IAskMkiPint> Pints { get; }

  public IAskMkiPpu? Ppu { get; }

  public IAskMkiPki? Pki { get; }

  public static LegacyAskDeviceComposition Create(LegacyMkiHardwareProfile profile, IAskMkiController controller, int numberChassis)
  {
    ArgumentNullException.ThrowIfNull(profile);
    ArgumentNullException.ThrowIfNull(controller);

    var acp = Attach(new ASKMKI_ACP(), numberChassis);
    var commutator = Attach(new ASKMKI_Commutator(), numberChassis);
    var timer = Attach(new ASKMKI_Timer(), numberChassis);
    var pints = new Dictionary<int, IAskMkiPint>();

    AddPintIfConfigured(profile, numberChassis, pints, 3);
    AddPintIfConfigured(profile, numberChassis, pints, 4);

    IAskMkiPpu? ppu = profile.HardwareConfig.TyPpu == 0 ? null : Attach(new ASKMKI_PPU(), numberChassis);
    IAskMkiPki? pki = profile.HardwareConfig.IsPki == 0 ? null : Attach(new ASKMKI_PKI(), numberChassis);

    LogInformation(
      $"АСК composition: стойка={numberChassis}, ПИНТ={string.Join(",", pints.Keys)}, ППУ={(ppu == null ? "нет" : "да")}, ПКИ={(pki == null ? "нет" : "да")}",
      isDeviceLog: true);

    return new LegacyAskDeviceComposition(controller, acp, commutator, timer, pints, ppu, pki);
  }

  public IAskMkiPint GetRequiredPint(int pint)
  {
    if (Pints.TryGetValue(pint, out var device))
    {
      return device;
    }

    throw new InvalidOperationException($"В конфигурации стойки АСК не найден ПИНТ{pint}.");
  }

  private static void AddPintIfConfigured(LegacyMkiHardwareProfile profile, int numberChassis, Dictionary<int, IAskMkiPint> pints, int pint)
  {
    byte type = profile.HardwareConfig.GuiType.ElementAtOrDefault(pint - 3);
    if (type == 0)
    {
      return;
    }

    IAskMkiPint device = pint == 3
      ? CreatePint3(type)
      : CreatePint4(type);

    pints[pint] = Attach(device, numberChassis);
    LogInformation($"АСК composition: ПИНТ{pint}, type={type}, device={device.GetType().Name}", isDeviceLog: true);
  }

  private static IAskMkiPint CreatePint3(byte type)
  {
    return type == 1 ? new ASKMKI_PINT3_B5108() : new ASKMKI_PINT3_Zup();
  }

  private static IAskMkiPint CreatePint4(byte type)
  {
    return type == 1 ? new ASKMKI_PINT4_PUI() : new ASKMKI_PINT4_RS485();
  }

  private static T Attach<T>(T device, int numberChassis)
    where T : IAskMkiAttachableDevice
  {
    device.NumberChassis = numberChassis;
    return device;
  }
}
