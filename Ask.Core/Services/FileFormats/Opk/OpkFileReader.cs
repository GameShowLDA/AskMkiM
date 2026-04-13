using System.IO;

namespace Ask.Core.Services.FileFormats.Opk
{
  internal sealed class OpkFileReader
  {
    private static readonly byte[] VrMarkerRecord = { 0x02, 0x02, 0x00 };
    private static readonly byte[] VrMarkerOrdinary = { 0x02, 0x15, 0x00 };
    private static readonly byte[] VrMarkerEnd = { 0x01, 0x21, 0x00 };

    private readonly byte[] _defaultSignature;

    public OpkFileReader(byte[] defaultSignature)
    {
      _defaultSignature = defaultSignature;
    }

    public OpkFileContent Read(string path)
    {
      var bytes = File.ReadAllBytes(path);
      if (bytes.Length < _defaultSignature.Length + sizeof(uint))
      {
        throw new InvalidDataException("Файл слишком короткий для OPK.");
      }

      var actualCrc = BitConverter.ToUInt32(bytes, bytes.Length - sizeof(uint));
      var body = bytes[..^sizeof(uint)];
      var expectedCrc = ComputeCrc32(body);
      if (actualCrc != expectedCrc)
      {
        throw new InvalidDataException("CRC OPK-файла не совпадает.");
      }

      if (!TryResolveSignatureAndOffset(bytes, out var offset, out var signature))
      {
        throw new InvalidDataException("Не удалось определить начало VR-блоков в OPK.");
      }

      var vkeyBlock = ReadVrBlock(bytes, ref offset);
      var vbinBlock = ReadVrBlock(bytes, ref offset);
      var vtxtBlock = ReadVrBlock(bytes, ref offset);

      if (offset != bytes.Length - sizeof(uint))
      {
        throw new InvalidDataException("После чтения VR-блоков обнаружены лишние данные.");
      }

      return new OpkFileContent(
        signature,
        vkeyBlock.RawBytes,
        vbinBlock.RawBytes,
        vtxtBlock.RawBytes,
        ParseTextRecords(vtxtBlock.PayloadBytes));
    }

    private bool TryResolveSignatureAndOffset(byte[] bytes, out int offset, out byte[] signature)
    {
      if (bytes.Length > _defaultSignature.Length + 1 && LooksLikeVrMarker(bytes, _defaultSignature.Length))
      {
        signature = new byte[_defaultSignature.Length];
        Buffer.BlockCopy(bytes, 0, signature, 0, signature.Length);
        offset = signature.Length;
        return true;
      }

      return TryFindFirstVrOffset(bytes, out offset, out signature);
    }

    private static bool TryFindFirstVrOffset(byte[] bytes, out int offset, out byte[] signature)
    {
      var limit = Math.Min(bytes.Length - sizeof(uint), 512);
      for (var candidate = 1; candidate < limit; candidate++)
      {
        if (!LooksLikeVrMarker(bytes, candidate))
        {
          continue;
        }

        try
        {
          var probeOffset = candidate;
          ReadVrBlock(bytes, ref probeOffset);
          ReadVrBlock(bytes, ref probeOffset);
          ReadVrBlock(bytes, ref probeOffset);

          if (probeOffset != bytes.Length - sizeof(uint))
          {
            continue;
          }

          offset = candidate;
          signature = new byte[candidate];
          Buffer.BlockCopy(bytes, 0, signature, 0, candidate);
          return true;
        }
        catch
        {
          // Пробуем следующий кандидат.
        }
      }

      offset = 0;
      signature = Array.Empty<byte>();
      return false;
    }

    private static VrBlock ReadVrBlock(byte[] fileBytes, ref int offset)
    {
      var start = offset;
      using var stream = new MemoryStream(fileBytes, offset, fileBytes.Length - offset, writable: false);
      using var reader = new BinaryReader(stream);

      var payloadBytes = ReadVrPayload(reader);
      var consumed = (int)stream.Position;
      var rawBytes = new byte[consumed];
      Buffer.BlockCopy(fileBytes, start, rawBytes, 0, consumed);

      offset += consumed;
      return new VrBlock(rawBytes, payloadBytes);
    }

    private static byte[] ReadVrPayload(BinaryReader reader)
    {
      using var capture = new MemoryStream();
      using var writer = new BinaryWriter(capture);

      var marker = reader.ReadBytes(3);
      if (marker.Length != 3)
      {
        throw new EndOfStreamException("Не удалось прочитать маркер VR-блока.");
      }

      writer.Write(marker);

      var isRecord = marker.SequenceEqual(VrMarkerRecord);
      var isOrdinary = marker.SequenceEqual(VrMarkerOrdinary);
      if (!isRecord && !isOrdinary)
      {
        throw new InvalidDataException("Неизвестный маркер VR-блока.");
      }

      var recordCount = reader.ReadUInt32();
      writer.Write(recordCount);

      if (isRecord)
      {
        var recordSize = reader.ReadUInt16();
        writer.Write(recordSize);

        for (uint index = 0; index < recordCount; index++)
        {
          var recordBytes = reader.ReadBytes(recordSize);
          if (recordBytes.Length != recordSize)
          {
            throw new EndOfStreamException("Оборван блок фиксированных записей VR.");
          }

          writer.Write(recordBytes);
        }
      }
      else
      {
        for (uint index = 0; index < recordCount; index++)
        {
          var keyLength = reader.ReadUInt16();
          var dataLength = reader.ReadUInt16();
          var childCount = reader.ReadUInt16();

          writer.Write(keyLength);
          writer.Write(dataLength);
          writer.Write(childCount);

          var keyBytes = reader.ReadBytes(keyLength);
          if (keyBytes.Length != keyLength)
          {
            throw new EndOfStreamException("Оборван ключ VR-записи.");
          }

          writer.Write(keyBytes);

          var dataBytes = reader.ReadBytes(dataLength);
          if (dataBytes.Length != dataLength)
          {
            throw new EndOfStreamException("Оборваны данные VR-записи.");
          }

          writer.Write(dataBytes);

          for (uint childIndex = 0; childIndex < childCount; childIndex++)
          {
            writer.Write(ReadVrPayload(reader));
          }
        }
      }

      var repeatedRecordCount = reader.ReadUInt32();
      writer.Write(repeatedRecordCount);
      if (repeatedRecordCount != recordCount)
      {
        throw new InvalidDataException("Контрольное количество записей VR не совпадает.");
      }

      var endMarker = reader.ReadBytes(3);
      if (endMarker.Length != 3)
      {
        throw new EndOfStreamException("Оборван конец VR-блока.");
      }

      writer.Write(endMarker);
      if (!endMarker.SequenceEqual(VrMarkerEnd))
      {
        throw new InvalidDataException("Неверный маркер конца VR-блока.");
      }

      writer.Flush();
      return capture.ToArray();
    }

    private static IReadOnlyList<byte[]> ParseTextRecords(byte[] vtxtPayload)
    {
      using var stream = new MemoryStream(vtxtPayload, writable: false);
      using var reader = new BinaryReader(stream);

      var marker = reader.ReadBytes(3);
      if (marker.Length != 3)
      {
        throw new EndOfStreamException("Не удалось прочитать маркер vtxt.");
      }

      var isRecord = marker.SequenceEqual(VrMarkerRecord);
      var isOrdinary = marker.SequenceEqual(VrMarkerOrdinary);
      if (!isRecord && !isOrdinary)
      {
        throw new InvalidDataException("Неизвестный маркер блока vtxt.");
      }

      var recordCount = reader.ReadUInt32();
      var records = new List<byte[]>((int)recordCount);

      if (isRecord)
      {
        var recordSize = reader.ReadUInt16();
        for (uint index = 0; index < recordCount; index++)
        {
          var recordBytes = reader.ReadBytes(recordSize);
          if (recordBytes.Length != recordSize)
          {
            throw new EndOfStreamException("Оборван блок фиксированных записей vtxt.");
          }

          records.Add(recordBytes);
        }
      }
      else
      {
        for (uint index = 0; index < recordCount; index++)
        {
          var keyLength = reader.ReadUInt16();
          var dataLength = reader.ReadUInt16();
          var childCount = reader.ReadUInt16();

          if (keyLength > 0)
          {
            var keyBytes = reader.ReadBytes(keyLength);
            if (keyBytes.Length != keyLength)
            {
              throw new EndOfStreamException("Оборван ключ в блоке vtxt.");
            }
          }

          var dataBytes = reader.ReadBytes(dataLength);
          if (dataBytes.Length != dataLength)
          {
            throw new EndOfStreamException("Оборваны данные в блоке vtxt.");
          }

          records.Add(dataBytes);

          for (uint childIndex = 0; childIndex < childCount; childIndex++)
          {
            SkipVr(reader);
          }
        }
      }

      var repeatedRecordCount = reader.ReadUInt32();
      if (repeatedRecordCount != recordCount)
      {
        throw new InvalidDataException("Контрольное количество записей vtxt не совпадает.");
      }

      var endMarker = reader.ReadBytes(3);
      if (!endMarker.SequenceEqual(VrMarkerEnd))
      {
        throw new InvalidDataException("Неверный маркер завершения vtxt.");
      }

      if (reader.BaseStream.Position != reader.BaseStream.Length)
      {
        throw new InvalidDataException("После чтения vtxt остались лишние байты.");
      }

      return records;
    }

    private static void SkipVr(BinaryReader reader)
    {
      var marker = reader.ReadBytes(3);
      if (marker.Length != 3)
      {
        throw new EndOfStreamException("Не удалось пропустить вложенный VR-блок.");
      }

      var isRecord = marker.SequenceEqual(VrMarkerRecord);
      var isOrdinary = marker.SequenceEqual(VrMarkerOrdinary);
      if (!isRecord && !isOrdinary)
      {
        throw new InvalidDataException("Неизвестный маркер вложенного VR-блока.");
      }

      var recordCount = reader.ReadUInt32();

      if (isRecord)
      {
        var recordSize = reader.ReadUInt16();
        reader.BaseStream.Seek((long)recordSize * recordCount, SeekOrigin.Current);
      }
      else
      {
        for (uint index = 0; index < recordCount; index++)
        {
          var keyLength = reader.ReadUInt16();
          var dataLength = reader.ReadUInt16();
          var childCount = reader.ReadUInt16();

          reader.BaseStream.Seek(keyLength + dataLength, SeekOrigin.Current);
          for (uint childIndex = 0; childIndex < childCount; childIndex++)
          {
            SkipVr(reader);
          }
        }
      }

      var repeatedRecordCount = reader.ReadUInt32();
      var endMarker = reader.ReadBytes(3);
      if (repeatedRecordCount != recordCount || !endMarker.SequenceEqual(VrMarkerEnd))
      {
        throw new InvalidDataException("Повреждён вложенный VR-блок.");
      }
    }

    private static bool LooksLikeVrMarker(byte[] bytes, int offset)
    {
      if (offset + 2 >= bytes.Length)
      {
        return false;
      }

      var isRecord = bytes[offset] == VrMarkerRecord[0]
                     && bytes[offset + 1] == VrMarkerRecord[1]
                     && bytes[offset + 2] == VrMarkerRecord[2];
      var isOrdinary = bytes[offset] == VrMarkerOrdinary[0]
                       && bytes[offset + 1] == VrMarkerOrdinary[1]
                       && bytes[offset + 2] == VrMarkerOrdinary[2];

      return isRecord || isOrdinary;
    }

    private static uint ComputeCrc32(byte[] bytes)
    {
      const uint polynomial = 0xEDB88320u;
      var crc = ~0u;

      foreach (var currentByte in bytes)
      {
        crc ^= currentByte;
        for (var bit = 0; bit < 8; bit++)
        {
          var carry = (crc & 1u) != 0;
          crc >>= 1;
          if (carry)
          {
            crc ^= polynomial;
          }
        }
      }

      return ~crc;
    }

    private sealed class VrBlock
    {
      public VrBlock(byte[] rawBytes, byte[] payloadBytes)
      {
        RawBytes = rawBytes;
        PayloadBytes = payloadBytes;
      }

      public byte[] RawBytes { get; }

      public byte[] PayloadBytes { get; }
    }
  }

  internal sealed class OpkFileContent
  {
    public OpkFileContent(
      byte[] signatureBytes,
      byte[] vkeyBlock,
      byte[] vbinBlock,
      byte[] vtxtBlock,
      IReadOnlyList<byte[]> textRecords)
    {
      SignatureBytes = signatureBytes;
      VkeyBlock = vkeyBlock;
      VbinBlock = vbinBlock;
      VtxtBlock = vtxtBlock;
      TextRecords = textRecords;
    }

    public byte[] SignatureBytes { get; }

    public byte[] VkeyBlock { get; }

    public byte[] VbinBlock { get; }

    public byte[] VtxtBlock { get; }

    public IReadOnlyList<byte[]> TextRecords { get; }
  }
}
