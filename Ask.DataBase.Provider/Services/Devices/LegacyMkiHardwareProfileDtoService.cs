using Ask.Core.Services.Config.LegacyMki;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.DataBase.Provider.Context;
using Ask.DataBase.Provider.Initialization;
using Ask.DataBase.Provider.Services.Base;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Ask.DataBase.Provider.Services.Devices;

/// <summary>
/// Сервис работы с legacy-конфигурациями аппаратуры АСК-МКИ.
/// </summary>
public sealed class LegacyMkiHardwareProfileDtoService : CrudService<LegacyMkiHardwareProfileDto>
{
  /// <summary>
  /// Возвращает профиль legacy-конфигурации для указанной стойки и типа профиля.
  /// </summary>
  public async Task<LegacyMkiHardwareProfileDto?> GetByChassisAsync(
    int numberChassis,
    LegacyMkiProfileKind profileKind,
    CancellationToken cancellationToken = default)
  {
    await using var context = CreateContext();
    await EnsureStorageReadyAsync(cancellationToken);

    return await context.LegacyMkiHardwareProfiles
      .FirstOrDefaultAsync(
        x => x.NumberChassis == numberChassis && x.ProfileKind == profileKind,
        cancellationToken);
  }

  /// <summary>
  /// Создает или обновляет профиль legacy-конфигурации для указанной стойки.
  /// </summary>
  public async Task<LegacyMkiHardwareProfileDto> SaveProfileAsync(
    int numberChassis,
    LegacyMkiProfileKind profileKind,
    LegacyMkiHardwareProfile profile,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(profile);

    await using var context = CreateContext();
    await EnsureStorageReadyAsync(cancellationToken);
    var entity = LegacyMkiHardwareProfileDto.FromProfile(numberChassis, profileKind, profile);

    var existing = await context.LegacyMkiHardwareProfiles
      .FirstOrDefaultAsync(
        x => x.NumberChassis == numberChassis && x.ProfileKind == profileKind,
        cancellationToken);

    if (existing == null)
    {
      context.LegacyMkiHardwareProfiles.Add(entity);
      await context.SaveChangesAsync(cancellationToken);
      return entity;
    }

    entity.Id = existing.Id;
    context.Entry(existing).CurrentValues.SetValues(entity);
    await context.SaveChangesAsync(cancellationToken);
    return existing;
  }

  /// <summary>
  /// Создает таблицу хранения legacy-профилей, если она отсутствует в текущей базе.
  /// </summary>
  public async Task EnsureStorageAsync(CancellationToken cancellationToken = default)
  {
    await using var context = CreateContext();
    await EnsureStorageReadyAsync(cancellationToken);
  }

  /// <summary>
  /// Заполняет отсутствующие или ошибочно созданные legacy-профили стандартной конфигурацией старой системы.
  /// </summary>
  public async Task EnsureDefaultProfilesAsync(int numberChassis, CancellationToken cancellationToken = default)
  {
    var defaultConfig = TryLoadDefaultHardwareConfig();
    if (defaultConfig == null)
    {
      return;
    }

    await using var context = CreateContext();
    await EnsureStorageReadyAsync(cancellationToken);

    foreach (LegacyMkiProfileKind profileKind in Enum.GetValues(typeof(LegacyMkiProfileKind)))
    {
      var existing = await context.LegacyMkiHardwareProfiles
        .FirstOrDefaultAsync(
          x => x.NumberChassis == numberChassis && x.ProfileKind == profileKind,
          cancellationToken);

      if (existing != null && !IsBlankLegacyProfile(existing) && !IsKnownWrongDefaultSeed(existing))
      {
        continue;
      }

      var defaultProfile = defaultConfig.GetProfile(profileKind);
      var entity = LegacyMkiHardwareProfileDto.FromProfile(numberChassis, profileKind, defaultProfile);

      if (existing == null)
      {
        context.LegacyMkiHardwareProfiles.Add(entity);
      }
      else
      {
        entity.Id = existing.Id;
        context.Entry(existing).CurrentValues.SetValues(entity);
      }
    }

    await context.SaveChangesAsync(cancellationToken);
  }

  /// <summary>
  /// Загружает стандартную конфигурацию старой системы из известных проверенных расположений.
  /// </summary>
  private static LegacyMkiHardwareConfigFile? TryLoadDefaultHardwareConfig()
  {
    string[] candidatePaths =
    [
      @"C:\MKI\OUTW\mki_hrd.cfg",
      @"C:\MKI\OUT\MKI_HRD.CFG",
      @"C:\MKI\mki_hrd.cfg"
    ];

    foreach (var path in candidatePaths)
    {
      if (!File.Exists(path))
      {
        continue;
      }

      try
      {
        var config = LegacyMkiHardwareConfigFileService.Load(path);
        NormalizeStandardDefaultConfig(config);
        return config;
      }
      catch
      {
        // Если конкретный файл поврежден или занят, пробуем следующий источник стандартной конфигурации.
      }
    }

    return null;
  }

  /// <summary>
  /// Приводит стандартные значения СК/БК к виду, который используется в старой программе по умолчанию.
  /// </summary>
  private static void NormalizeStandardDefaultConfig(LegacyMkiHardwareConfigFile config)
  {
    foreach (var profile in config.Profiles)
    {
      profile.HardwareConfig.SkIs = [1, 0, 0, 0, 0, 0, 0, 0];
      profile.HardwareConfig.SkBkBeg = [1, 1, 1, 1, 1, 1, 1, 1];
      profile.HardwareConfig.SkBkEnd = [12, 24, 24, 24, 24, 24, 24, 24];
    }
  }

  /// <summary>
  /// Определяет, что профиль еще не заполнен реальными настройками.
  /// </summary>
  private static bool IsBlankLegacyProfile(LegacyMkiHardwareProfileDto profile)
  {
    return profile.DvAcp == 0
      && profile.DvV7 == 0
      && profile.EtGui4 == 0
      && profile.TyPpu == 0
      && profile.PkiUmax == 0
      && profile.U220 == 0
      && profile.SkPwr == 0
      && profile.BkBus == 0
      && profile.GuiPwr == 0
      && profile.V7Gat == 0
      && profile.PkiPwr == 0
      && string.IsNullOrWhiteSpace(profile.Password0)
      && string.IsNullOrWhiteSpace(profile.Password1)
      && IsZeroBlob(profile.SkIs)
      && IsZeroBlob(profile.SkBkBeg)
      && IsZeroBlob(profile.SkBkEnd)
      && IsZeroBlob(profile.GuiType)
      && IsZeroBlob(profile.PortSku)
      && IsZeroBlob(profile.PortVm)
      && IsZeroBlob(profile.PortGui3);
  }

  /// <summary>
  /// Проверяет, что бинарное поле отсутствует или содержит только нули.
  /// </summary>
  private static bool IsZeroBlob(byte[]? bytes)
  {
    return bytes == null || bytes.Length == 0 || bytes.All(value => value == 0);
  }

  /// <summary>
  /// Распознает ранее созданные некорректные стандартные профили, которые нужно перезаписать.
  /// </summary>
  private static bool IsKnownWrongDefaultSeed(LegacyMkiHardwareProfileDto profile)
  {
    var oldRootDefault =
      profile.DvAcp == 1
      && profile.GuiAmperMax.Length >= sizeof(double)
      && Math.Abs(BitConverter.ToDouble(profile.GuiAmperMax, 0) - 6.0) < 0.000001
      && Math.Abs(profile.RwirAdc) < 0.000001
      && profile.Comx4Com1 == 0
      && profile.V7Bef == 600;

    var outwDefaultWithExtraSwitch =
      profile.DvAcp == 2
      && profile.DvV7 == 6
      && profile.SkIs.Length > 1
      && profile.SkIs[0] == 1
      && profile.SkIs[1] == 1
      && Math.Abs(profile.RwirAdc - 2.0) < 0.000001
      && profile.Comx4Com1 == 1;

    return oldRootDefault || outwDefaultWithExtraSwitch;
  }

  /// <summary>
  /// Создает физическую таблицу и индекс legacy-профилей в указанном контексте.
  /// </summary>
  private static async Task EnsureStorageReadyAsync(CancellationToken cancellationToken)
  {
    await DatabaseInitializationService.EnsureLegacyMkiHardwareProfilesStorageAsync(cancellationToken);
  }

  private static string ResolveSqliteType(Type type)
  {
    type = Nullable.GetUnderlyingType(type) ?? type;

    if (type == typeof(string) || type == typeof(DateTime) || type.IsEnum)
    {
      return "TEXT";
    }

    if (type == typeof(byte[]))
    {
      return "BLOB";
    }

    if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
    {
      return "REAL";
    }

    return "INTEGER";
  }

  /// <summary>
  /// Возвращает безопасное значение по умолчанию для добавляемой SQLite-колонки.
  /// </summary>
  private static string ResolveSqliteDefault(string sqliteType)
  {
    return sqliteType switch
    {
      "TEXT" => "''",
      "BLOB" => "X''",
      "REAL" => "0",
      _ => "0"
    };
  }

  /// <summary>
  /// Экранирует имя таблицы, индекса или колонки для SQLite.
  /// </summary>
  private static string Quote(string identifier)
  {
    return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
  }
}
