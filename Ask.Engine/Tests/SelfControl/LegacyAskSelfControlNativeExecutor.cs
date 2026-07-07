using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.DataBase.Provider.Services.Devices;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Запускает нативно переписанные модули самоконтроля старого тестера АСК.
/// </summary>
public sealed class LegacyAskSelfControlNativeExecutor
{
  private readonly IReadOnlyDictionary<LegacyAskSelfControlModule, LegacyAskModuleTestBase> _tests;

  /// <summary>
  /// Создаёт исполнитель нативного самоконтроля АСК.
  /// </summary>
  public LegacyAskSelfControlNativeExecutor()
  {
    LegacyAskModuleTestBase[] tests =
    [
      new LegacyAskDigitalVoltmeterSelfControlTest(),
      new LegacyAskAdcSelfControlTest(),
      new LegacyAskDeviceSwitchingSelfControlTest(),
      new LegacyAskPintsSelfControlTest(),
      new LegacyAskCommutatorSelfControlTest(),
      new LegacyAskPpuSelfControlTest(),
      new LegacyAskPkiSelfControlTest(),
      new LegacyAskTimerSelfControlTest()
    ];

    _tests = tests.ToDictionary(test => test.Module);
  }

  /// <summary>
  /// Выполняет выбранный модуль самоконтроля.
  /// </summary>
  public async Task ExecuteAsync(
    LegacyAskSelfControlTarget target,
    IUserInteractionService messageService,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(target);
    ArgumentNullException.ThrowIfNull(messageService);

    if (!_tests.TryGetValue(target.Module, out var test))
    {
      await messageService.ShowMessageAsync(new ShowMessageModel("Ошибка самоконтроля АСК", message: "Не найден обработчик выбранного модуля.", type: ShowMessageModel.MessageType.Error));
      return;
    }

    var service = new LegacyMkiHardwareProfileDtoService();
    await service.EnsureDefaultProfilesAsync(target.NumberChassis, cancellationToken);

    var profileKind = LegacyMkiConfig.GetSelectedProfile();
    var profileDto = await service.GetByChassisAsync(target.NumberChassis, profileKind, cancellationToken);
    if (profileDto == null)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel("Ошибка самоконтроля АСК", message: $"Не найдена конфигурация стойки {target.NumberChassis}.", type: ShowMessageModel.MessageType.Error));
      return;
    }

    var context = new LegacyAskSelfControlContext(target, profileDto.ToProfile(), messageService, cancellationToken);
    await test.ExecuteAsync(context);
  }
}
