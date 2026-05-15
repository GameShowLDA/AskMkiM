using System.Text.Json.Serialization;

namespace Ask.Core.Services.Config.LegacyMki;

/// <summary>
/// Одна сохранённая аппаратная конфигурация из mki_hrd.cfg.
/// </summary>
public sealed class LegacyMkiHardwareProfile
{
  public LegacyMkiHardwareConfigSection HardwareConfig { get; set; } = new();

  public LegacyMkiHardwareAuxSection HardwareAux { get; set; } = new();

  public LegacyMkiTimingSection Timing { get; set; } = new();

  public string[] Passwords { get; set; } = new string[2];

  [JsonIgnore]
  internal byte[] HardwareConfigReserved { get; set; } = new byte[50];

  [JsonIgnore]
  internal byte[] HardwareAuxReserved { get; set; } = new byte[40];

  [JsonIgnore]
  internal ushort[] TimingReserved { get; set; } = new ushort[12];
}

public sealed class LegacyMkiHardwareConfigSection
{
  public byte Nas { get; set; }
  public byte DvAcp { get; set; }
  public byte DvV7 { get; set; }
  public byte EtGui4 { get; set; }
  public byte[] SkIs { get; set; } = new byte[8];
  public byte[] SkBkBeg { get; set; } = new byte[8];
  public byte[] SkBkEnd { get; set; } = new byte[8];
  public byte[] GuiType { get; set; } = new byte[2];
  public double[] GuiVoltStep { get; set; } = new double[2];
  public double[] GuiAmperStep { get; set; } = new double[2];
  public double[] GuiVoltMax { get; set; } = new double[2];
  public double[] GuiAmperMax { get; set; } = new double[2];
  public byte KuGui4 { get; set; }
  public byte IsRos { get; set; }
  public double GomCmt { get; set; }
  public byte TyPpu { get; set; }
  public ushort PkiUmax { get; set; }
  public byte AcpTmr { get; set; }
  public byte NAcpMaMax { get; set; }
  public byte IsPki { get; set; }
  public byte Comx4Com1 { get; set; }
  public byte BbSpr { get; set; }
  public byte LcIs { get; set; }
  public double RbusBb { get; set; }
  public byte PkiExtMo { get; set; }
  public byte AcpIs0_3V { get; set; }
  public byte DivGatBk { get; set; }
  public double UmaxEk { get; set; }
  public byte EkFull { get; set; }
  public ushort UmaxSiEkFull { get; set; }
  public ushort UmaxPiEkFull { get; set; }
  public byte CalcPgr { get; set; }
}

public sealed class LegacyMkiHardwareAuxSection
{
  public byte Res1 { get; set; }
  public byte IsTstUpki { get; set; }
  public ushort U220 { get; set; }
  public double[] PkiAkomDiv { get; set; } = new double[8];
  public double RwirAdc { get; set; }
  public double[] PkiKomTst { get; set; } = new double[10];
  public double PpuKmul { get; set; }
  public double UacpR { get; set; }
  public double Uv7R { get; set; }
  public byte Net { get; set; }
  public byte BeepOff { get; set; }
  public byte Meas2 { get; set; }
  public double RwirV7 { get; set; }
  public double Rgui4 { get; set; }
  public double DIGui4mA { get; set; }
  public ushort KopAddr { get; set; }
  public double KmulKi { get; set; }
  public byte LocErrSob { get; set; }
  public byte ShortSsRt { get; set; }
  public double[] PkiAVolt { get; set; } = new double[5];
  public byte UseWait { get; set; }
  public byte ReioVm { get; set; }
  public LegacyMkiPortSettings PortSku { get; set; } = new();
  public LegacyMkiPortSettings PortVm { get; set; } = new();
  public LegacyMkiPortSettings PortFs { get; set; } = new();
  public byte QMeasC { get; set; }
  public byte OutUpi { get; set; }
  public ushort MksAcpTmr { get; set; }
  public string UsbAddrVm { get; set; } = string.Empty;
  public double TdobTdo { get; set; }
  public double TdobTi { get; set; }
  public short CcorrectPf { get; set; }
  public byte ReioGui3 { get; set; }
  public LegacyMkiPortSettings PortGui3 { get; set; } = new();
}

public sealed class LegacyMkiTimingSection
{
  public ushort SkPwr { get; set; }
  public ushort BkBus { get; set; }
  public ushort EkRk { get; set; }
  public ushort PtEk { get; set; }
  public ushort PtRk { get; set; }
  public ushort EpPwr { get; set; }
  public ushort KzSh { get; set; }
  public ushort GuiPwr { get; set; }
  public ushort Gui4Mod { get; set; }
  public ushort GuiGat { get; set; }
  public ushort V734Mod { get; set; }
  public ushort V753Mod { get; set; }
  public ushort V765Mod { get; set; }
  public ushort V7Gat { get; set; }
  public ushort AcpMod { get; set; }
  public ushort AcpGat { get; set; }
  public ushort PkiPwr { get; set; }
  public ushort PkiMod { get; set; }
  public ushort PpuPwr { get; set; }
  public ushort PpuMod { get; set; }
  public ushort KoPwr { get; set; }
  public ushort EpBef { get; set; }
  public ushort V7Bef { get; set; }
  public ushort AcpBef { get; set; }
  public ushort PkiBef { get; set; }
  public ushort PpuBef { get; set; }
  public ushort V753RunR { get; set; }
  public ushort V753RunU { get; set; }
  public ushort V753RunV { get; set; }
  public ushort V765RunR { get; set; }
  public ushort V765RunU { get; set; }
  public ushort V765RunUv { get; set; }
  public ushort Gui3Mod { get; set; }
  public ushort[] Reserv1 { get; set; } = new ushort[3];
  public ushort GuiRst { get; set; }
  public ushort LcBef { get; set; }
  public ushort PpuAftPusk { get; set; }
  public ushort TMeasUppuMin { get; set; }
}

public sealed class LegacyMkiPortSettings
{
  public byte Com1 { get; set; }
  public byte Baud { get; set; }
  public byte Parity { get; set; }
  public byte Protocol { get; set; }
  public byte QStopBit { get; set; }
  public byte RtsDtr { get; set; }
  public ushort MsTmo { get; set; }
  public ushort MksWait { get; set; }
  public byte[] Reserved { get; set; } = new byte[8];
  public byte Len { get; set; }
  public ushort Base { get; set; }
}
