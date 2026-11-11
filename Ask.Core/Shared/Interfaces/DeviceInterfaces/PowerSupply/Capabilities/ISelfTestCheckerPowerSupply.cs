using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities
{
  public interface ISelfTestCheckerPowerSupply
  {
    /// <summary>
    /// Запускает выбранную проверку источника питания.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены проверки.</param>
    /// <param name="selectedType">Тип проверки источника питания.</param>
    /// <param name="settings">Параметры запуска и накопитель результатов самоконтроля.</param>
    /// <param name="userMessageService">Сервис вывода сообщений в протокол и взаимодействия с оператором.</param>
    /// <param name="powerSupply">Проверяемый источник питания.</param>
    /// <param name="meter">Мультиметр для измерения выходной величины.</param>
    /// <returns>Асинхронная задача выполнения проверки.</returns>
    Task StartSelfCheck(CancellationToken cancellationToken, Enum? selectedType, ActionSettings settings,
      IUserInteractionService userMessageService, IPowerSupply powerSupply, IMultimeter meter);

    /// <summary>
    /// Возвращает тип перечисления, используемый как тип проверки.
    /// </summary>
    Type GetTestTypeEnum();
  }
}
