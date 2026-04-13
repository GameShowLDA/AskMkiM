using System.IO;

namespace Ask.Core.Services.FileFormats.Opk
{
  /// <summary>
  /// Выполняет чтение OPK-файла и извлечение его основных VR-блоков.
  /// </summary>
  internal sealed class OpkFileReader
  {
    /// <summary>
    /// Хранит маркер VR-блока с записями фиксированного размера.
    /// </summary>
    private static readonly byte[] VrMarkerRecord = { 0x02, 0x02, 0x00 };

    /// <summary>
    /// Хранит маркер обычного VR-блока.
    /// </summary>
    private static readonly byte[] VrMarkerOrdinary = { 0x02, 0x15, 0x00 };

    /// <summary>
    /// Хранит маркер завершения VR-блока.
    /// </summary>
    private static readonly byte[] VrMarkerEnd = { 0x01, 0x21, 0x00 };

    /// <summary>
    /// Хранит сигнатуру OPK-файла, используемую по умолчанию при определении смещения VR-блоков.
    /// </summary>
    private readonly byte[] _defaultSignature;

    /// <summary>
    /// Инициализирует новый экземпляр читателя OPK-файлов.
    /// </summary>
    /// <param name="defaultSignature">Сигнатура OPK-файла по умолчанию.</param>
    public OpkFileReader(byte[] defaultSignature)
    {
      _defaultSignature = defaultSignature;
    }

    /// <summary>
    /// Считывает OPK-файл и извлекает из него текстовые и бинарные блоки.
    /// </summary>
    /// <param name="path">Путь к OPK-файлу.</param>
    /// <returns>Извлечённое содержимое OPK-файла.</returns>
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

    /// <summary>
    /// Определяет сигнатуру OPK-файла и смещение первого VR-блока.
    /// </summary>
    /// <param name="bytes">Байты исходного OPK-файла.</param>
    /// <param name="offset">Смещение первого VR-блока.</param>
    /// <param name="signature">Определённая сигнатура файла.</param>
    /// <returns><see langword="true"/>, если сигнатура и смещение определены; иначе <see langword="false"/>.</returns>
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

    /// <summary>
    /// Пытается найти смещение первого VR-блока методом перебора допустимых позиций.
    /// </summary>
    /// <param name="bytes">Байты исходного OPK-файла.</param>
    /// <param name="offset">Найденное смещение первого VR-блока.</param>
    /// <param name="signature">Сигнатура, предшествующая первому VR-блоку.</param>
    /// <returns><see langword="true"/>, если смещение найдено; иначе <see langword="false"/>.</returns>
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

    /// <summary>
    /// Считывает один VR-блок из массива байтов, начиная с указанного смещения.
    /// </summary>
    /// <param name="fileBytes">Байты OPK-файла.</param>
    /// <param name="offset">Текущее смещение чтения, которое обновляется после завершения метода.</param>
    /// <returns>Сырые и распакованные данные VR-блока.</returns>
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

    /// <summary>
    /// Считывает полезную нагрузку VR-блока с сохранением исходной структуры.
    /// </summary>
    /// <param name="reader">Читатель бинарных данных.</param>
    /// <returns>Полезная нагрузка VR-блока.</returns>
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

    /// <summary>
    /// Извлекает текстовые записи из блока vtxt.
    /// </summary>
    /// <param name="vtxtPayload">Полезная нагрузка блока vtxt.</param>
    /// <returns>Коллекция текстовых записей.</returns>
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

    /// <summary>
    /// Пропускает вложенный VR-блок без сохранения его содержимого.
    /// </summary>
    /// <param name="reader">Читатель бинарных данных.</param>
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

    /// <summary>
    /// Проверяет, начинается ли указанный участок массива байтов с допустимого маркера VR-блока.
    /// </summary>
    /// <param name="bytes">Проверяемый массив байтов.</param>
    /// <param name="offset">Смещение, с которого выполняется проверка.</param>
    /// <returns><see langword="true"/>, если по смещению найден маркер VR-блока; иначе <see langword="false"/>.</returns>
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

    /// <summary>
    /// Вычисляет контрольную сумму CRC32 для указанного массива байтов.
    /// </summary>
    /// <param name="bytes">Байты, для которых требуется вычислить CRC32.</param>
    /// <returns>Значение CRC32.</returns>
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

    /// <summary>
    /// Представляет VR-блок в сыром и распакованном виде.
    /// </summary>
    private sealed class VrBlock
    {
      /// <summary>
      /// Инициализирует новый экземпляр описания VR-блока.
      /// </summary>
      /// <param name="rawBytes">Сырые байты VR-блока.</param>
      /// <param name="payloadBytes">Полезная нагрузка VR-блока.</param>
      public VrBlock(byte[] rawBytes, byte[] payloadBytes)
      {
        RawBytes = rawBytes;
        PayloadBytes = payloadBytes;
      }

      /// <summary>
      /// Получает сырое байтовое представление VR-блока.
      /// </summary>
      public byte[] RawBytes { get; }

      /// <summary>
      /// Получает полезную нагрузку VR-блока.
      /// </summary>
      public byte[] PayloadBytes { get; }
    }
  }

  /// <summary>
  /// Содержит данные, извлечённые из OPK-файла.
  /// </summary>
  internal sealed class OpkFileContent
  {
    /// <summary>
    /// Инициализирует новый экземпляр данных OPK-файла.
    /// </summary>
    /// <param name="signatureBytes">Сигнатура OPK-файла.</param>
    /// <param name="vkeyBlock">Блок vkey.</param>
    /// <param name="vbinBlock">Блок vbin.</param>
    /// <param name="vtxtBlock">Блок vtxt.</param>
    /// <param name="textRecords">Текстовые записи из блока vtxt.</param>
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

    /// <summary>
    /// Получает сигнатуру исходного OPK-файла.
    /// </summary>
    public byte[] SignatureBytes { get; }

    /// <summary>
    /// Получает блок vkey.
    /// </summary>
    public byte[] VkeyBlock { get; }

    /// <summary>
    /// Получает блок vbin.
    /// </summary>
    public byte[] VbinBlock { get; }

    /// <summary>
    /// Получает блок vtxt.
    /// </summary>
    public byte[] VtxtBlock { get; }

    /// <summary>
    /// Получает текстовые записи, извлечённые из блока vtxt.
    /// </summary>
    public IReadOnlyList<byte[]> TextRecords { get; }
  }
}
