using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;

namespace Ask.Core.Services.Config.LegacyMki;

/// <summary>
/// Читает и сохраняет legacy-файл mki_hrd.cfg.
/// </summary>
public static class LegacyMkiHardwareConfigFileService
{
  private static readonly Encoding OemEncoding;
  private static readonly byte[] VrBeginMarker = { 0x02, 0x15, 0x00 };
  private static readonly byte[] VrEndMarker = { 0x01, 0x21, 0x00 };
  private const string ExpectedFileName = "mki_hrd.cfg";
  private const ushort Version = 1;

  static LegacyMkiHardwareConfigFileService()
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    OemEncoding = Encoding.GetEncoding(866);
  }

  public static LegacyMkiHardwareConfigFile Load(string path)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(path);

    var data = File.ReadAllBytes(path);
    if (data.Length < 32)
    {
      throw new InvalidDataException("Файл mki_hrd.cfg слишком короткий.");
    }

    var storedCrc = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(data.Length - sizeof(uint)));
    var calculatedCrc = ComputeCrc32(data.AsSpan(0, data.Length - sizeof(uint)));
    if (storedCrc != calculatedCrc)
    {
      throw new InvalidDataException("Контрольная сумма mki_hrd.cfg не совпадает.");
    }

    var reader = new SpanReader(data);
    ValidatePreamble(ref reader);

    var recordLengthStart = reader.ReadUInt16();
    if (recordLengthStart != sizeof(byte))
    {
      throw new InvalidDataException("Неверный формат заголовка активного профиля.");
    }

    var activeProfileIndex = reader.ReadByte();
    var recordLengthEnd = reader.ReadUInt16();
    if (recordLengthEnd != sizeof(byte))
    {
      throw new InvalidDataException("Неверный формат заголовка активного профиля.");
    }

    var marker = reader.ReadBytes(VrBeginMarker.Length);
    if (!marker.SequenceEqual(VrBeginMarker))
    {
      throw new InvalidDataException("Неверный формат блока профилей.");
    }

    var profileCount = reader.ReadUInt32();
    if (profileCount != LegacyMkiHardwareConfigFile.ProfileCount)
    {
      throw new InvalidDataException($"Ожидалось {LegacyMkiHardwareConfigFile.ProfileCount} профиля, найдено {profileCount}.");
    }

    var file = new LegacyMkiHardwareConfigFile
    {
      ActiveProfileIndex = activeProfileIndex
    };

    for (var i = 0; i < profileCount; i++)
    {
      var keyLength = reader.ReadUInt16();
      var dataLength = reader.ReadUInt16();
      var childCount = reader.ReadUInt16();
      if (keyLength != 0 || childCount != 0 || dataLength == 0)
      {
        throw new InvalidDataException("Неподдерживаемый формат записи профиля в mki_hrd.cfg.");
      }

      var profileBytes = reader.ReadBytes(dataLength);
      file.SetProfile((LegacyMkiProfileKind)i, ReadProfile(profileBytes, (byte)i));
    }

    var repeatedProfileCount = reader.ReadUInt32();
    if (repeatedProfileCount != profileCount)
    {
      throw new InvalidDataException("Неверный хвост блока профилей.");
    }

    var endMarker = reader.ReadBytes(VrEndMarker.Length);
    if (!endMarker.SequenceEqual(VrEndMarker))
    {
      throw new InvalidDataException("Неверный маркер завершения блока профилей.");
    }

    ValidatePreamble(ref reader);

    if (!reader.IsAtEnd(sizeof(uint)))
    {
      throw new InvalidDataException("Файл mki_hrd.cfg содержит неожиданные данные.");
    }

    return file;
  }

  public static void Save(string path, LegacyMkiHardwareConfigFile file)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    ArgumentNullException.ThrowIfNull(file);

    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true);

    WritePreamble(writer);
    writer.Write((ushort)sizeof(byte));
    writer.Write(file.ActiveProfileIndex);
    writer.Write((ushort)sizeof(byte));
    writer.Write(VrBeginMarker);
    writer.Write((uint)LegacyMkiHardwareConfigFile.ProfileCount);

    foreach (var profile in file.Profiles)
    {
      var profileBytes = WriteProfile(profile);
      writer.Write((ushort)0);
      writer.Write((ushort)profileBytes.Length);
      writer.Write((ushort)0);
      writer.Write(profileBytes);
    }

    writer.Write((uint)LegacyMkiHardwareConfigFile.ProfileCount);
    writer.Write(VrEndMarker);
    WritePreamble(writer);

    writer.Flush();
    var payload = ms.ToArray();
    var crc = ComputeCrc32(payload);

    using var fileStream = File.Create(path);
    using var fileWriter = new BinaryWriter(fileStream, Encoding.ASCII, leaveOpen: false);
    fileWriter.Write(payload);
    fileWriter.Write(crc);
  }

  private static void ValidatePreamble(ref SpanReader reader)
  {
    var fileNameLength = reader.ReadUInt16();
    var fileName = Encoding.ASCII.GetString(reader.ReadBytes(fileNameLength)).TrimEnd('\0');
    var version = reader.ReadUInt16();

    if (!string.Equals(fileName, ExpectedFileName, StringComparison.OrdinalIgnoreCase) || version != Version)
    {
      throw new InvalidDataException("Файл не является поддерживаемым mki_hrd.cfg.");
    }
  }

  private static void WritePreamble(BinaryWriter writer)
  {
    var fileNameBytes = Encoding.ASCII.GetBytes(ExpectedFileName + '\0');
    writer.Write((ushort)fileNameBytes.Length);
    writer.Write(fileNameBytes);
    writer.Write(Version);
  }

  private static LegacyMkiHardwareProfile ReadProfile(ReadOnlySpan<byte> data, byte nas)
  {
    var reader = new SpanReader(data);
    var profile = new LegacyMkiHardwareProfile();
    var hcfg = profile.HardwareConfig;
    var haux = profile.HardwareAux;
    var timing = profile.Timing;

    hcfg.Nas = reader.ReadByte();
    hcfg.DvAcp = reader.ReadByte();
    hcfg.DvV7 = reader.ReadByte();
    hcfg.EtGui4 = reader.ReadByte();
    hcfg.SkIs = reader.ReadByteArray(8);
    hcfg.SkBkBeg = reader.ReadByteArray(8);
    hcfg.SkBkEnd = reader.ReadByteArray(8);
    hcfg.GuiType = reader.ReadByteArray(2);
    hcfg.GuiVoltStep = reader.ReadDoubleArray(2);
    hcfg.GuiAmperStep = reader.ReadDoubleArray(2);
    hcfg.GuiVoltMax = reader.ReadDoubleArray(2);
    hcfg.GuiAmperMax = reader.ReadDoubleArray(2);
    hcfg.KuGui4 = reader.ReadByte();
    hcfg.IsRos = reader.ReadByte();
    hcfg.GomCmt = reader.ReadDouble();
    hcfg.TyPpu = reader.ReadByte();
    hcfg.PkiUmax = reader.ReadUInt16();
    hcfg.AcpTmr = reader.ReadByte();
    hcfg.NAcpMaMax = reader.ReadByte();
    hcfg.IsPki = reader.ReadByte();
    hcfg.Comx4Com1 = reader.ReadByte();
    hcfg.BbSpr = reader.ReadByte();
    hcfg.LcIs = reader.ReadByte();
    hcfg.RbusBb = reader.ReadDouble();
    hcfg.PkiExtMo = reader.ReadByte();
    hcfg.AcpIs0_3V = reader.ReadByte();
    hcfg.DivGatBk = reader.ReadByte();
    hcfg.UmaxEk = reader.ReadDouble();
    hcfg.EkFull = reader.ReadByte();
    hcfg.UmaxSiEkFull = reader.ReadUInt16();
    hcfg.UmaxPiEkFull = reader.ReadUInt16();
    hcfg.CalcPgr = reader.ReadByte();
    profile.HardwareConfigReserved = reader.ReadByteArray(50);

    haux.Res1 = reader.ReadByte();
    haux.IsTstUpki = reader.ReadByte();
    haux.U220 = reader.ReadUInt16();
    haux.PkiAkomDiv = reader.ReadDoubleArray(8);
    haux.RwirAdc = reader.ReadDouble();
    haux.PkiKomTst = reader.ReadDoubleArray(10);
    haux.PpuKmul = reader.ReadDouble();
    haux.UacpR = reader.ReadDouble();
    haux.Uv7R = reader.ReadDouble();
    haux.Net = reader.ReadByte();
    haux.BeepOff = reader.ReadByte();
    haux.Meas2 = reader.ReadByte();
    haux.RwirV7 = reader.ReadDouble();
    haux.Rgui4 = reader.ReadDouble();
    haux.DIGui4mA = reader.ReadDouble();
    haux.KopAddr = reader.ReadUInt16();
    haux.KmulKi = reader.ReadDouble();
    haux.LocErrSob = reader.ReadByte();
    haux.ShortSsRt = reader.ReadByte();
    haux.PkiAVolt = reader.ReadDoubleArray(5);
    haux.UseWait = reader.ReadByte();
    haux.ReioVm = reader.ReadByte();
    haux.PortSku = ReadPort(ref reader);
    haux.PortVm = ReadPort(ref reader);
    haux.PortFs = ReadPort(ref reader);
    haux.QMeasC = reader.ReadByte();
    haux.OutUpi = reader.ReadByte();
    haux.MksAcpTmr = reader.ReadUInt16();
    haux.UsbAddrVm = reader.ReadFixedString(81, OemEncoding);
    haux.TdobTdo = reader.ReadDouble();
    haux.TdobTi = reader.ReadDouble();
    haux.CcorrectPf = reader.ReadInt16();
    haux.ReioGui3 = reader.ReadByte();
    haux.PortGui3 = ReadPort(ref reader);
    profile.HardwareAuxReserved = reader.ReadByteArray(40);

    timing.SkPwr = reader.ReadUInt16();
    timing.BkBus = reader.ReadUInt16();
    timing.EkRk = reader.ReadUInt16();
    timing.PtEk = reader.ReadUInt16();
    timing.PtRk = reader.ReadUInt16();
    timing.EpPwr = reader.ReadUInt16();
    timing.KzSh = reader.ReadUInt16();
    timing.GuiPwr = reader.ReadUInt16();
    timing.Gui4Mod = reader.ReadUInt16();
    timing.GuiGat = reader.ReadUInt16();
    timing.V734Mod = reader.ReadUInt16();
    timing.V753Mod = reader.ReadUInt16();
    timing.V765Mod = reader.ReadUInt16();
    timing.V7Gat = reader.ReadUInt16();
    timing.AcpMod = reader.ReadUInt16();
    timing.AcpGat = reader.ReadUInt16();
    timing.PkiPwr = reader.ReadUInt16();
    timing.PkiMod = reader.ReadUInt16();
    timing.PpuPwr = reader.ReadUInt16();
    timing.PpuMod = reader.ReadUInt16();
    timing.KoPwr = reader.ReadUInt16();
    timing.EpBef = reader.ReadUInt16();
    timing.V7Bef = reader.ReadUInt16();
    timing.AcpBef = reader.ReadUInt16();
    timing.PkiBef = reader.ReadUInt16();
    timing.PpuBef = reader.ReadUInt16();
    timing.V753RunR = reader.ReadUInt16();
    timing.V753RunU = reader.ReadUInt16();
    timing.V753RunV = reader.ReadUInt16();
    timing.V765RunR = reader.ReadUInt16();
    timing.V765RunU = reader.ReadUInt16();
    timing.V765RunUv = reader.ReadUInt16();
    timing.Gui3Mod = reader.ReadUInt16();
    timing.Reserv1 = reader.ReadUInt16Array(3);
    timing.GuiRst = reader.ReadUInt16();
    timing.LcBef = reader.ReadUInt16();
    timing.PpuAftPusk = reader.ReadUInt16();
    timing.TMeasUppuMin = reader.ReadUInt16();
    profile.TimingReserved = reader.ReadUInt16Array(12);

    profile.Passwords = new[]
    {
      reader.ReadFixedString(9, OemEncoding),
      reader.ReadFixedString(9, OemEncoding)
    };

    hcfg.Nas = nas;
    return profile;
  }

  private static byte[] WriteProfile(LegacyMkiHardwareProfile profile)
  {
    using var ms = new MemoryStream();
    using var writer = new BinaryWriter(ms, OemEncoding, leaveOpen: true);
    var hcfg = profile.HardwareConfig;
    var haux = profile.HardwareAux;
    var timing = profile.Timing;

    writer.Write(hcfg.Nas);
    writer.Write(hcfg.DvAcp);
    writer.Write(hcfg.DvV7);
    writer.Write(hcfg.EtGui4);
    writer.Write(EnsureLength(hcfg.SkIs, 8));
    writer.Write(EnsureLength(hcfg.SkBkBeg, 8));
    writer.Write(EnsureLength(hcfg.SkBkEnd, 8));
    writer.Write(EnsureLength(hcfg.GuiType, 2));
    WriteDoubleArray(writer, EnsureLength(hcfg.GuiVoltStep, 2));
    WriteDoubleArray(writer, EnsureLength(hcfg.GuiAmperStep, 2));
    WriteDoubleArray(writer, EnsureLength(hcfg.GuiVoltMax, 2));
    WriteDoubleArray(writer, EnsureLength(hcfg.GuiAmperMax, 2));
    writer.Write(hcfg.KuGui4);
    writer.Write(hcfg.IsRos);
    writer.Write(hcfg.GomCmt);
    writer.Write(hcfg.TyPpu);
    writer.Write(hcfg.PkiUmax);
    writer.Write(hcfg.AcpTmr);
    writer.Write(hcfg.NAcpMaMax);
    writer.Write(hcfg.IsPki);
    writer.Write(hcfg.Comx4Com1);
    writer.Write(hcfg.BbSpr);
    writer.Write(hcfg.LcIs);
    writer.Write(hcfg.RbusBb);
    writer.Write(hcfg.PkiExtMo);
    writer.Write(hcfg.AcpIs0_3V);
    writer.Write(hcfg.DivGatBk);
    writer.Write(hcfg.UmaxEk);
    writer.Write(hcfg.EkFull);
    writer.Write(hcfg.UmaxSiEkFull);
    writer.Write(hcfg.UmaxPiEkFull);
    writer.Write(hcfg.CalcPgr);
    writer.Write(EnsureLength(profile.HardwareConfigReserved, 50));

    writer.Write(haux.Res1);
    writer.Write(haux.IsTstUpki);
    writer.Write(haux.U220);
    WriteDoubleArray(writer, EnsureLength(haux.PkiAkomDiv, 8));
    writer.Write(haux.RwirAdc);
    WriteDoubleArray(writer, EnsureLength(haux.PkiKomTst, 10));
    writer.Write(haux.PpuKmul);
    writer.Write(haux.UacpR);
    writer.Write(haux.Uv7R);
    writer.Write(haux.Net);
    writer.Write(haux.BeepOff);
    writer.Write(haux.Meas2);
    writer.Write(haux.RwirV7);
    writer.Write(haux.Rgui4);
    writer.Write(haux.DIGui4mA);
    writer.Write(haux.KopAddr);
    writer.Write(haux.KmulKi);
    writer.Write(haux.LocErrSob);
    writer.Write(haux.ShortSsRt);
    WriteDoubleArray(writer, EnsureLength(haux.PkiAVolt, 5));
    writer.Write(haux.UseWait);
    writer.Write(haux.ReioVm);
    WritePort(writer, haux.PortSku);
    WritePort(writer, haux.PortVm);
    WritePort(writer, haux.PortFs);
    writer.Write(haux.QMeasC);
    writer.Write(haux.OutUpi);
    writer.Write(haux.MksAcpTmr);
    WriteFixedString(writer, haux.UsbAddrVm, 81, OemEncoding);
    writer.Write(haux.TdobTdo);
    writer.Write(haux.TdobTi);
    writer.Write(haux.CcorrectPf);
    writer.Write(haux.ReioGui3);
    WritePort(writer, haux.PortGui3);
    writer.Write(EnsureLength(profile.HardwareAuxReserved, 40));

    WriteUInt16Array(writer, new[]
    {
      timing.SkPwr, timing.BkBus, timing.EkRk, timing.PtEk, timing.PtRk, timing.EpPwr, timing.KzSh,
      timing.GuiPwr, timing.Gui4Mod, timing.GuiGat, timing.V734Mod, timing.V753Mod, timing.V765Mod,
      timing.V7Gat, timing.AcpMod, timing.AcpGat, timing.PkiPwr, timing.PkiMod, timing.PpuPwr,
      timing.PpuMod, timing.KoPwr, timing.EpBef, timing.V7Bef, timing.AcpBef, timing.PkiBef,
      timing.PpuBef, timing.V753RunR, timing.V753RunU, timing.V753RunV, timing.V765RunR,
      timing.V765RunU, timing.V765RunUv, timing.Gui3Mod
    });
    WriteUInt16Array(writer, EnsureLength(timing.Reserv1, 3));
    writer.Write(timing.GuiRst);
    writer.Write(timing.LcBef);
    writer.Write(timing.PpuAftPusk);
    writer.Write(timing.TMeasUppuMin);
    WriteUInt16Array(writer, EnsureLength(profile.TimingReserved, 12));

    var passwords = profile.Passwords ?? Array.Empty<string>();
    WriteFixedString(writer, passwords.ElementAtOrDefault(0) ?? string.Empty, 9, OemEncoding);
    WriteFixedString(writer, passwords.ElementAtOrDefault(1) ?? string.Empty, 9, OemEncoding);

    writer.Flush();
    return ms.ToArray();
  }

  private static LegacyMkiPortSettings ReadPort(ref SpanReader reader)
  {
    return new LegacyMkiPortSettings
    {
      Com1 = reader.ReadByte(),
      Baud = reader.ReadByte(),
      Parity = reader.ReadByte(),
      Protocol = reader.ReadByte(),
      QStopBit = reader.ReadByte(),
      RtsDtr = reader.ReadByte(),
      MsTmo = reader.ReadUInt16(),
      MksWait = reader.ReadUInt16(),
      Reserved = reader.ReadByteArray(8),
      Len = reader.ReadByte(),
      Base = reader.ReadUInt16()
    };
  }

  private static void WritePort(BinaryWriter writer, LegacyMkiPortSettings? port)
  {
    port ??= new LegacyMkiPortSettings();
    writer.Write(port.Com1);
    writer.Write(port.Baud);
    writer.Write(port.Parity);
    writer.Write(port.Protocol);
    writer.Write(port.QStopBit);
    writer.Write(port.RtsDtr);
    writer.Write(port.MsTmo);
    writer.Write(port.MksWait);
    writer.Write(EnsureLength(port.Reserved, 8));
    writer.Write(port.Len);
    writer.Write(port.Base);
  }

  private static void WriteDoubleArray(BinaryWriter writer, double[] values)
  {
    foreach (var value in values)
    {
      writer.Write(value);
    }
  }

  private static void WriteUInt16Array(BinaryWriter writer, ushort[] values)
  {
    foreach (var value in values)
    {
      writer.Write(value);
    }
  }

  private static void WriteFixedString(BinaryWriter writer, string value, int byteLength, Encoding encoding)
  {
    var buffer = new byte[byteLength];
    if (!string.IsNullOrEmpty(value))
    {
      var encoded = encoding.GetBytes(value);
      Array.Copy(encoded, buffer, Math.Min(byteLength - 1, encoded.Length));
    }

    writer.Write(buffer);
  }

  private static T[] EnsureLength<T>(T[]? values, int expectedLength)
  {
    var result = new T[expectedLength];
    if (values != null)
    {
      Array.Copy(values, result, Math.Min(values.Length, expectedLength));
    }

    return result;
  }

  private static uint ComputeCrc32(ReadOnlySpan<byte> data)
  {
    var crc = 0u;
    foreach (var currentByte in data)
    {
      crc = ~crc;
      crc ^= currentByte;

      for (var i = 0; i < 8; i++)
      {
        var hasBit = (crc & 1) != 0;
        crc >>= 1;
        if (hasBit)
        {
          crc ^= 0xEDB88320u;
        }
      }

      crc = ~crc;
    }

    return crc;
  }

  private ref struct SpanReader
  {
    private readonly ReadOnlySpan<byte> _span;
    private int _offset;

    public SpanReader(ReadOnlySpan<byte> span)
    {
      _span = span;
      _offset = 0;
    }

    public bool IsAtEnd(int remainingBytes = 0) => _offset == _span.Length - remainingBytes;

    public byte ReadByte()
    {
      EnsureAvailable(1);
      return _span[_offset++];
    }

    public byte[] ReadBytes(int count)
    {
      EnsureAvailable(count);
      var result = _span.Slice(_offset, count).ToArray();
      _offset += count;
      return result;
    }

    public ushort ReadUInt16()
    {
      EnsureAvailable(sizeof(ushort));
      var value = BinaryPrimitives.ReadUInt16LittleEndian(_span.Slice(_offset, sizeof(ushort)));
      _offset += sizeof(ushort);
      return value;
    }

    public uint ReadUInt32()
    {
      EnsureAvailable(sizeof(uint));
      var value = BinaryPrimitives.ReadUInt32LittleEndian(_span.Slice(_offset, sizeof(uint)));
      _offset += sizeof(uint);
      return value;
    }

    public short ReadInt16()
    {
      EnsureAvailable(sizeof(short));
      var value = BinaryPrimitives.ReadInt16LittleEndian(_span.Slice(_offset, sizeof(short)));
      _offset += sizeof(short);
      return value;
    }

    public double ReadDouble()
    {
      EnsureAvailable(sizeof(double));
      var value = BitConverter.ToDouble(_span.Slice(_offset, sizeof(double)));
      _offset += sizeof(double);
      return value;
    }

    public byte[] ReadByteArray(int count)
    {
      var result = new byte[count];
      for (var i = 0; i < count; i++)
      {
        result[i] = ReadByte();
      }

      return result;
    }

    public ushort[] ReadUInt16Array(int count)
    {
      var result = new ushort[count];
      for (var i = 0; i < count; i++)
      {
        result[i] = ReadUInt16();
      }

      return result;
    }

    public double[] ReadDoubleArray(int count)
    {
      var result = new double[count];
      for (var i = 0; i < count; i++)
      {
        result[i] = ReadDouble();
      }

      return result;
    }

    public string ReadFixedString(int byteLength, Encoding encoding)
    {
      EnsureAvailable(byteLength);
      var value = encoding.GetString(_span.Slice(_offset, byteLength)).TrimEnd('\0', ' ');
      _offset += byteLength;
      return value;
    }

    private void EnsureAvailable(int count)
    {
      if (_offset + count > _span.Length)
      {
        throw new InvalidDataException("Файл mki_hrd.cfg повреждён или обрезан.");
      }
    }
  }
}
