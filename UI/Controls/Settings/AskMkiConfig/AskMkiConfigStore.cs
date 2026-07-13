using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.DataBase.Provider.Services.Devices;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace UI.Controls.Settings.AskMkiConfig;

/// <summary>
/// Загрузка и сохранение профиля АСК в базе.
/// </summary>
public partial class AskMkiConfigControl
{
  /// <summary>
  /// Сохраняет профиль для текущей legacy-стойки АСК.
  /// </summary>
  private async Task SaveProfileToDatabaseAsync(LegacyMkiHardwareProfile profile)
  {
    var numberChassis = await TryResolveLegacyAskChassisNumberAsync();
    if (numberChassis == null)
    {
      return;
    }

    var service = new LegacyMkiHardwareProfileDtoService();
    await service.SaveProfileAsync(numberChassis.Value, _selectedProfileKind, profile);
  }

  /// <summary>
  /// Подставляет в рабочий файл профиль, сохраненный в базе данных для текущей стойки.
  /// </summary>
  private void ApplyProfileFromDatabaseIfExists()
  {
    if (_loadedConfigFile == null)
    {
      return;
    }

    var numberChassis = TryResolveLegacyAskChassisNumberAsync()
      .GetAwaiter()
      .GetResult();

    if (numberChassis == null)
    {
      return;
    }

    var service = new LegacyMkiHardwareProfileDtoService();
    service.EnsureDefaultProfilesAsync(numberChassis.Value)
      .GetAwaiter()
      .GetResult();

    var profileDto = service.GetByChassisAsync(numberChassis.Value, _selectedProfileKind)
      .GetAwaiter()
      .GetResult();

    if (profileDto == null)
    {
      return;
    }

    _loadedConfigFile.SetProfile(_selectedProfileKind, profileDto.ToProfile());
  }

  /// <summary>
  /// Возвращает номер выбранной legacy-стойки АСК или находит первую такую стойку в базе.
  /// </summary>
  private async Task<int?> TryResolveLegacyAskChassisNumberAsync()
  {
    if (_numberChassis != null)
    {
      return _numberChassis.Value;
    }

    var service = new ChassisManagerDtoService();
    var chassis = await service.GetAllAsync();
    var legacyAsk = chassis.FirstOrDefault(x =>
      string.Equals(x.Name, "\u0422\u0435\u0441\u0442\u0435\u0440 \u0410\u0421\u041a", StringComparison.OrdinalIgnoreCase) ||
      x.DeviceClass.EndsWith(".ManagerASKMKI", StringComparison.Ordinal));

    return legacyAsk?.Number;
  }
}
