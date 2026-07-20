using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Interfaces.ProtocolInterfaces;
using Ask.UI.Controls.ProtocolNew;

namespace Ask.UI.Features.ProtocolNew.Protocol;

/// <summary>
/// Выполняет завершающие операции потокового и итогового протоколов после окончания процесса.
/// </summary>
internal sealed class ProtocolCompletionService
{
  /// <summary>
  /// Построитель текста итогового протокола.
  /// </summary>
  private readonly IInspectionProtocolBuilder _inspectionProtocolBuilder;

  /// <summary>
  /// Инициализирует сервис завершения протоколов.
  /// </summary>
  /// <param name="inspectionProtocolBuilder">Построитель итогового текста.</param>
  public ProtocolCompletionService(IInspectionProtocolBuilder inspectionProtocolBuilder)
  {
    _inspectionProtocolBuilder = inspectionProtocolBuilder;
  }

  /// <summary>
  /// Выполняет настроенную печать текущего потокового протокола.
  /// </summary>
  /// <param name="protocol">Компонент, содержащий записи выполнения.</param>
  public void PrintIfRequired(ProtocolUI protocol)
  {
    if (ProtocolConfig.GetPrintProtocol())
    {
      PrintUtility.PrintProtocol(protocol.GetShowMessageModels());
    }
  }

  /// <summary>
  /// Показывает сообщение о завершении и при необходимости формирует итоговый протокол режима.
  /// </summary>
  /// <param name="settings">Настройки и результаты завершённого действия.</param>
  /// <param name="protocol">Компонент отображения протокола.</param>
  public async Task DisplayCompletionAsync(ActionSettings settings, ProtocolUI protocol)
  {
    var completionMessage = new ShowMessageModel
    {
      Header = "Завершено",
      CanBeDeleted = false,
    };

    protocol.LastMessage = true;
    if (settings.CheckType == CheckType.ControlProgram)
    {
      return;
    }

    await protocol.ShowMessageAsync(completionMessage, ignoreOutputValidation: true);
    if (settings.CheckType == CheckType.Metrology)
    {
      return;
    }

    await protocol.AppendEmptyLineAsync();
    protocol.ShowInspectionProtocol(_inspectionProtocolBuilder.Build(settings));
  }

  /// <summary>
  /// Сохраняет оба представления протокола и показывает панель управления сохранёнными файлами.
  /// </summary>
  /// <param name="protocol">Компонент отображения и хранения протокола.</param>
  public async Task SaveAndExposeAsync(ProtocolUI protocol)
  {
    await protocol.SaveProtocolAsync(protocol.Header);
    await protocol.SaveInspectionProtocolAsync(protocol.Header);
    protocol.ShowProtocolManager();
  }
}
