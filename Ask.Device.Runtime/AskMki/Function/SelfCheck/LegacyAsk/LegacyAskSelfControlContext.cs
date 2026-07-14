using Ask.Core.Services.Config.LegacyMki;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Содержит данные, необходимые нативному самоконтролю старого тестера АСК.
/// </summary>
public sealed class LegacyAskSelfControlContext
{
  /// <summary>
  /// Создаёт контекст выполнения самоконтроля.
  /// </summary>
  public LegacyAskSelfControlContext(
    LegacyAskSelfControlTarget target,
    LegacyMkiHardwareProfile profile,
    IUserInteractionService messageService,
    CancellationToken cancellationToken,
    IMultimeter? multimeter = null)
  {
    Target = target;
    Profile = profile;
    MessageService = messageService;
    Reporter = new LegacyAskSelfControlReporter(messageService);
    CancellationToken = cancellationToken;
    Multimeter = multimeter;
  }

  /// <summary>
  /// Возвращает выбранный пункт самоконтроля.
  /// </summary>
  public LegacyAskSelfControlTarget Target { get; }

  /// <summary>
  /// Возвращает аппаратную конфигурацию АСК из БД.
  /// </summary>
  public LegacyMkiHardwareProfile Profile { get; }

  /// <summary>
  /// Возвращает сервис вывода сообщений в протокол.
  /// </summary>
  public IUserInteractionService MessageService { get; }

  /// <summary>
  /// Возвращает writer стандартного протокола АСКМ.
  /// </summary>
  public LegacyAskSelfControlReporter Reporter { get; }

  /// <summary>
  /// Возвращает токен отмены выполнения.
  /// </summary>
  public CancellationToken CancellationToken { get; }

  /// <summary>
  /// Возвращает выбранный цифровой мультиметр для аппаратных измерений.
  /// </summary>
  public IMultimeter? Multimeter { get; }
}
