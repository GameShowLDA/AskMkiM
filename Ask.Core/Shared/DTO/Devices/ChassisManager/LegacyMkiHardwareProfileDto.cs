using Ask.Core.Services.Config.LegacyMki;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Ask.Core.Shared.DTO.Devices.ChassisManager;

/// <summary>
/// Запись legacy-конфигурации аппаратуры АСК-МКИ для конкретной стойки.
/// </summary>
public sealed class LegacyMkiHardwareProfileDto
{
  /// <summary>
  /// Возвращает или задает идентификатор записи.
  /// </summary>
  [Key]
  public int Id { get; set; }

  /// <summary>
  /// Возвращает или задает номер стойки, к которой относится профиль.
  /// </summary>
  public int NumberChassis { get; set; }

  /// <summary>
  /// Возвращает или задает тип legacy-профиля.
  /// </summary>
  public LegacyMkiProfileKind ProfileKind { get; set; }

  /// <summary>
  /// Возвращает или задает дату последнего обновления профиля.
  /// </summary>
  public DateTime UpdatedAtUtc { get; set; }

  public byte Nas { get; set; }
  public byte DvAcp { get; set; }
  public byte DvV7 { get; set; }
  public byte EtGui4 { get; set; }
  public byte[] SkIs { get; set; } = [];
  public byte[] SkBkBeg { get; set; } = [];
  public byte[] SkBkEnd { get; set; } = [];
  public byte[] GuiType { get; set; } = [];
  public byte[] GuiVoltStep { get; set; } = [];
  public byte[] GuiAmperStep { get; set; } = [];
  public byte[] GuiVoltMax { get; set; } = [];
  public byte[] GuiAmperMax { get; set; } = [];
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
  public byte[] HardwareConfigReserved { get; set; } = [];

  public byte Res1 { get; set; }
  public byte IsTstUpki { get; set; }
  public ushort U220 { get; set; }
  public byte[] PkiAkomDiv { get; set; } = [];
  public double RwirAdc { get; set; }
  public byte[] PkiKomTst { get; set; } = [];
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
  public byte[] PkiAVolt { get; set; } = [];
  public byte UseWait { get; set; }
  public byte ReioVm { get; set; }
  public byte[] PortSku { get; set; } = [];
  public byte[] PortVm { get; set; } = [];
  public byte[] PortFs { get; set; } = [];
  public byte QMeasC { get; set; }
  public byte OutUpi { get; set; }
  public ushort MksAcpTmr { get; set; }
  public string UsbAddrVm { get; set; } = string.Empty;
  public double TdobTdo { get; set; }
  public double TdobTi { get; set; }
  public short CcorrectPf { get; set; }
  public byte ReioGui3 { get; set; }
  public byte[] PortGui3 { get; set; } = [];
  public byte[] HardwareAuxReserved { get; set; } = [];

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
  public byte[] Reserv1 { get; set; } = [];
  public ushort GuiRst { get; set; }
  public ushort LcBef { get; set; }
  public ushort PpuAftPusk { get; set; }
  public ushort TMeasUppuMin { get; set; }
  public byte[] TimingReserved { get; set; } = [];
  public string Password0 { get; set; } = string.Empty;
  public string Password1 { get; set; } = string.Empty;

  /// <summary>
  /// Создает DTO из доменного профиля legacy-конфигурации.
  /// </summary>
  public static LegacyMkiHardwareProfileDto FromProfile(
    int numberChassis,
    LegacyMkiProfileKind profileKind,
    LegacyMkiHardwareProfile profile)
  {
    var hardware = profile.HardwareConfig;
    var aux = profile.HardwareAux;
    var timing = profile.Timing;

    return new LegacyMkiHardwareProfileDto
    {
      NumberChassis = numberChassis,
      ProfileKind = profileKind,
      UpdatedAtUtc = DateTime.UtcNow,
      Nas = hardware.Nas,
      DvAcp = hardware.DvAcp,
      DvV7 = hardware.DvV7,
      EtGui4 = hardware.EtGui4,
      SkIs = Copy(hardware.SkIs),
      SkBkBeg = Copy(hardware.SkBkBeg),
      SkBkEnd = Copy(hardware.SkBkEnd),
      GuiType = Copy(hardware.GuiType),
      GuiVoltStep = PackDoubles(hardware.GuiVoltStep),
      GuiAmperStep = PackDoubles(hardware.GuiAmperStep),
      GuiVoltMax = PackDoubles(hardware.GuiVoltMax),
      GuiAmperMax = PackDoubles(hardware.GuiAmperMax),
      KuGui4 = hardware.KuGui4,
      IsRos = hardware.IsRos,
      GomCmt = hardware.GomCmt,
      TyPpu = hardware.TyPpu,
      PkiUmax = hardware.PkiUmax,
      AcpTmr = hardware.AcpTmr,
      NAcpMaMax = hardware.NAcpMaMax,
      IsPki = hardware.IsPki,
      Comx4Com1 = hardware.Comx4Com1,
      BbSpr = hardware.BbSpr,
      LcIs = hardware.LcIs,
      RbusBb = hardware.RbusBb,
      PkiExtMo = hardware.PkiExtMo,
      AcpIs0_3V = hardware.AcpIs0_3V,
      DivGatBk = hardware.DivGatBk,
      UmaxEk = hardware.UmaxEk,
      EkFull = hardware.EkFull,
      UmaxSiEkFull = hardware.UmaxSiEkFull,
      UmaxPiEkFull = hardware.UmaxPiEkFull,
      CalcPgr = hardware.CalcPgr,
      HardwareConfigReserved = Copy(profile.HardwareConfigReserved),
      Res1 = aux.Res1,
      IsTstUpki = aux.IsTstUpki,
      U220 = aux.U220,
      PkiAkomDiv = PackDoubles(aux.PkiAkomDiv),
      RwirAdc = aux.RwirAdc,
      PkiKomTst = PackDoubles(aux.PkiKomTst),
      PpuKmul = aux.PpuKmul,
      UacpR = aux.UacpR,
      Uv7R = aux.Uv7R,
      Net = aux.Net,
      BeepOff = aux.BeepOff,
      Meas2 = aux.Meas2,
      RwirV7 = aux.RwirV7,
      Rgui4 = aux.Rgui4,
      DIGui4mA = aux.DIGui4mA,
      KopAddr = aux.KopAddr,
      KmulKi = aux.KmulKi,
      LocErrSob = aux.LocErrSob,
      ShortSsRt = aux.ShortSsRt,
      PkiAVolt = PackDoubles(aux.PkiAVolt),
      UseWait = aux.UseWait,
      ReioVm = aux.ReioVm,
      PortSku = PackPort(aux.PortSku),
      PortVm = PackPort(aux.PortVm),
      PortFs = PackPort(aux.PortFs),
      QMeasC = aux.QMeasC,
      OutUpi = aux.OutUpi,
      MksAcpTmr = aux.MksAcpTmr,
      UsbAddrVm = aux.UsbAddrVm ?? string.Empty,
      TdobTdo = aux.TdobTdo,
      TdobTi = aux.TdobTi,
      CcorrectPf = aux.CcorrectPf,
      ReioGui3 = aux.ReioGui3,
      PortGui3 = PackPort(aux.PortGui3),
      HardwareAuxReserved = Copy(profile.HardwareAuxReserved),
      SkPwr = timing.SkPwr,
      BkBus = timing.BkBus,
      EkRk = timing.EkRk,
      PtEk = timing.PtEk,
      PtRk = timing.PtRk,
      EpPwr = timing.EpPwr,
      KzSh = timing.KzSh,
      GuiPwr = timing.GuiPwr,
      Gui4Mod = timing.Gui4Mod,
      GuiGat = timing.GuiGat,
      V734Mod = timing.V734Mod,
      V753Mod = timing.V753Mod,
      V765Mod = timing.V765Mod,
      V7Gat = timing.V7Gat,
      AcpMod = timing.AcpMod,
      AcpGat = timing.AcpGat,
      PkiPwr = timing.PkiPwr,
      PkiMod = timing.PkiMod,
      PpuPwr = timing.PpuPwr,
      PpuMod = timing.PpuMod,
      KoPwr = timing.KoPwr,
      EpBef = timing.EpBef,
      V7Bef = timing.V7Bef,
      AcpBef = timing.AcpBef,
      PkiBef = timing.PkiBef,
      PpuBef = timing.PpuBef,
      V753RunR = timing.V753RunR,
      V753RunU = timing.V753RunU,
      V753RunV = timing.V753RunV,
      V765RunR = timing.V765RunR,
      V765RunU = timing.V765RunU,
      V765RunUv = timing.V765RunUv,
      Gui3Mod = timing.Gui3Mod,
      Reserv1 = PackUShorts(timing.Reserv1),
      GuiRst = timing.GuiRst,
      LcBef = timing.LcBef,
      PpuAftPusk = timing.PpuAftPusk,
      TMeasUppuMin = timing.TMeasUppuMin,
      TimingReserved = PackUShorts(profile.TimingReserved),
      Password0 = profile.Passwords.ElementAtOrDefault(0) ?? string.Empty,
      Password1 = profile.Passwords.ElementAtOrDefault(1) ?? string.Empty
    };
  }

  /// <summary>
  /// Восстанавливает доменный профиль legacy-конфигурации из DTO.
  /// </summary>
  public LegacyMkiHardwareProfile ToProfile()
  {
    return new LegacyMkiHardwareProfile
    {
      HardwareConfig = new LegacyMkiHardwareConfigSection
      {
        Nas = Nas,
        DvAcp = DvAcp,
        DvV7 = DvV7,
        EtGui4 = EtGui4,
        SkIs = UnpackBytes(SkIs, 8),
        SkBkBeg = UnpackBytes(SkBkBeg, 8),
        SkBkEnd = UnpackBytes(SkBkEnd, 8),
        GuiType = UnpackBytes(GuiType, 2),
        GuiVoltStep = UnpackDoubles(GuiVoltStep, 2),
        GuiAmperStep = UnpackDoubles(GuiAmperStep, 2),
        GuiVoltMax = UnpackDoubles(GuiVoltMax, 2),
        GuiAmperMax = UnpackDoubles(GuiAmperMax, 2),
        KuGui4 = KuGui4,
        IsRos = IsRos,
        GomCmt = GomCmt,
        TyPpu = TyPpu,
        PkiUmax = PkiUmax,
        AcpTmr = AcpTmr,
        NAcpMaMax = NAcpMaMax,
        IsPki = IsPki,
        Comx4Com1 = Comx4Com1,
        BbSpr = BbSpr,
        LcIs = LcIs,
        RbusBb = RbusBb,
        PkiExtMo = PkiExtMo,
        AcpIs0_3V = AcpIs0_3V,
        DivGatBk = DivGatBk,
        UmaxEk = UmaxEk,
        EkFull = EkFull,
        UmaxSiEkFull = UmaxSiEkFull,
        UmaxPiEkFull = UmaxPiEkFull,
        CalcPgr = CalcPgr
      },
      HardwareAux = new LegacyMkiHardwareAuxSection
      {
        Res1 = Res1,
        IsTstUpki = IsTstUpki,
        U220 = U220,
        PkiAkomDiv = UnpackDoubles(PkiAkomDiv, 8),
        RwirAdc = RwirAdc,
        PkiKomTst = UnpackDoubles(PkiKomTst, 10),
        PpuKmul = PpuKmul,
        UacpR = UacpR,
        Uv7R = Uv7R,
        Net = Net,
        BeepOff = BeepOff,
        Meas2 = Meas2,
        RwirV7 = RwirV7,
        Rgui4 = Rgui4,
        DIGui4mA = DIGui4mA,
        KopAddr = KopAddr,
        KmulKi = KmulKi,
        LocErrSob = LocErrSob,
        ShortSsRt = ShortSsRt,
        PkiAVolt = UnpackDoubles(PkiAVolt, 5),
        UseWait = UseWait,
        ReioVm = ReioVm,
        PortSku = UnpackPort(PortSku),
        PortVm = UnpackPort(PortVm),
        PortFs = UnpackPort(PortFs),
        QMeasC = QMeasC,
        OutUpi = OutUpi,
        MksAcpTmr = MksAcpTmr,
        UsbAddrVm = UsbAddrVm,
        TdobTdo = TdobTdo,
        TdobTi = TdobTi,
        CcorrectPf = CcorrectPf,
        ReioGui3 = ReioGui3,
        PortGui3 = UnpackPort(PortGui3)
      },
      Timing = new LegacyMkiTimingSection
      {
        SkPwr = SkPwr,
        BkBus = BkBus,
        EkRk = EkRk,
        PtEk = PtEk,
        PtRk = PtRk,
        EpPwr = EpPwr,
        KzSh = KzSh,
        GuiPwr = GuiPwr,
        Gui4Mod = Gui4Mod,
        GuiGat = GuiGat,
        V734Mod = V734Mod,
        V753Mod = V753Mod,
        V765Mod = V765Mod,
        V7Gat = V7Gat,
        AcpMod = AcpMod,
        AcpGat = AcpGat,
        PkiPwr = PkiPwr,
        PkiMod = PkiMod,
        PpuPwr = PpuPwr,
        PpuMod = PpuMod,
        KoPwr = KoPwr,
        EpBef = EpBef,
        V7Bef = V7Bef,
        AcpBef = AcpBef,
        PkiBef = PkiBef,
        PpuBef = PpuBef,
        V753RunR = V753RunR,
        V753RunU = V753RunU,
        V753RunV = V753RunV,
        V765RunR = V765RunR,
        V765RunU = V765RunU,
        V765RunUv = V765RunUv,
        Gui3Mod = Gui3Mod,
        Reserv1 = UnpackUShorts(Reserv1, 3),
        GuiRst = GuiRst,
        LcBef = LcBef,
        PpuAftPusk = PpuAftPusk,
        TMeasUppuMin = TMeasUppuMin
      },
      HardwareConfigReserved = UnpackBytes(HardwareConfigReserved, 50),
      HardwareAuxReserved = UnpackBytes(HardwareAuxReserved, 40),
      TimingReserved = UnpackUShorts(TimingReserved, 12),
      Passwords = [Password0, Password1]
    };
  }

  /// <summary>
  /// Возвращает копию массива байтов.
  /// </summary>
  private static byte[] Copy(byte[]? source)
  {
    return source == null ? [] : source.ToArray();
  }

  /// <summary>
  /// Упаковывает массив double в бинарное представление.
  /// </summary>
  private static byte[] PackDoubles(double[] values)
  {
    var bytes = new byte[values.Length * sizeof(double)];
    Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
    return bytes;
  }

  /// <summary>
  /// Восстанавливает массив double из бинарного представления.
  /// </summary>
  private static double[] UnpackDoubles(byte[]? bytes, int count)
  {
    var values = new double[count];
    if (bytes == null || bytes.Length == 0)
    {
      return values;
    }

    Buffer.BlockCopy(bytes, 0, values, 0, Math.Min(bytes.Length, values.Length * sizeof(double)));
    return values;
  }

  /// <summary>
  /// Упаковывает массив ushort в бинарное представление.
  /// </summary>
  private static byte[] PackUShorts(ushort[] values)
  {
    var bytes = new byte[values.Length * sizeof(ushort)];
    Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
    return bytes;
  }

  /// <summary>
  /// Восстанавливает массив ushort из бинарного представления.
  /// </summary>
  private static ushort[] UnpackUShorts(byte[]? bytes, int count)
  {
    var values = new ushort[count];
    if (bytes == null || bytes.Length == 0)
    {
      return values;
    }

    Buffer.BlockCopy(bytes, 0, values, 0, Math.Min(bytes.Length, values.Length * sizeof(ushort)));
    return values;
  }

  /// <summary>
  /// Восстанавливает массив байтов с ожидаемой длиной.
  /// </summary>
  private static byte[] UnpackBytes(byte[]? bytes, int count)
  {
    var values = new byte[count];
    if (bytes == null || bytes.Length == 0)
    {
      return values;
    }

    Array.Copy(bytes, values, Math.Min(bytes.Length, values.Length));
    return values;
  }

  /// <summary>
  /// Упаковывает настройки COM-порта в бинарное представление.
  /// </summary>
  private static byte[] PackPort(LegacyMkiPortSettings port)
  {
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);

    writer.Write(port.Com1);
    writer.Write(port.Baud);
    writer.Write(port.Parity);
    writer.Write(port.Protocol);
    writer.Write(port.QStopBit);
    writer.Write(port.RtsDtr);
    writer.Write(port.MsTmo);
    writer.Write(port.MksWait);
    writer.Write(UnpackBytes(port.Reserved, 8));
    writer.Write(port.Len);
    writer.Write(port.Base);

    return stream.ToArray();
  }

  /// <summary>
  /// Восстанавливает настройки COM-порта из бинарного представления.
  /// </summary>
  private static LegacyMkiPortSettings UnpackPort(byte[]? bytes)
  {
    if (bytes == null || bytes.Length == 0)
    {
      return new LegacyMkiPortSettings();
    }

    using var stream = new MemoryStream(bytes);
    using var reader = new BinaryReader(stream);

    return new LegacyMkiPortSettings
    {
      Com1 = ReadByte(reader),
      Baud = ReadByte(reader),
      Parity = ReadByte(reader),
      Protocol = ReadByte(reader),
      QStopBit = ReadByte(reader),
      RtsDtr = ReadByte(reader),
      MsTmo = ReadUInt16(reader),
      MksWait = ReadUInt16(reader),
      Reserved = ReadBytes(reader, 8),
      Len = ReadByte(reader),
      Base = ReadUInt16(reader)
    };
  }

  /// <summary>
  /// Безопасно читает byte из бинарного потока.
  /// </summary>
  private static byte ReadByte(BinaryReader reader)
  {
    return reader.BaseStream.Position < reader.BaseStream.Length ? reader.ReadByte() : (byte)0;
  }

  /// <summary>
  /// Безопасно читает ushort из бинарного потока.
  /// </summary>
  private static ushort ReadUInt16(BinaryReader reader)
  {
    return reader.BaseStream.Position + sizeof(ushort) <= reader.BaseStream.Length ? reader.ReadUInt16() : (ushort)0;
  }

  /// <summary>
  /// Безопасно читает массив байтов из бинарного потока.
  /// </summary>
  private static byte[] ReadBytes(BinaryReader reader, int count)
  {
    var bytes = reader.ReadBytes(Math.Min(count, (int)(reader.BaseStream.Length - reader.BaseStream.Position)));
    return UnpackBytes(bytes, count);
  }
}
