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
  /// <param name="settings">Настройки завершённого действия.</param>
  /// <param name="protocol">Компонент, содержащий записи выполнения.</param>
  public void PrintIfRequired(ActionSettings settings, ProtocolUI protocol)
  {
    if (settings.CheckType != CheckType.ControlProgram && ProtocolConfig.GetPrintProtocol())
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
  /// Добавляет обязательный финальный блок программы контроля в конец потокового протокола.
  /// </summary>
  /// <param name="settings">Параметры завершённого выполнения.</param>
  /// <param name="protocol">Компонент отображения протокола.</param>
  public async Task AppendControlProgramCompletionAsync(ActionSettings settings, ProtocolUI protocol)
  {
    if (settings.CheckType != CheckType.ControlProgram)
    {
      return;
    }

    await protocol.FinalizeCurrentCommandGroupAsync();
    var completionMessage = ControlProgramCompletionMessageBuilder.Build(settings);
    var successColor = ShowMessageModel.SuccessMessage.TitleColor;
    completionMessage.HeaderColor = successColor;
    completionMessage.MessageColor = successColor;
    completionMessage.TimeColor = successColor;
    await protocol.ShowMessageAsync(
      completionMessage,
      skipPause: true,
      ignoreOutputValidation: true);
  }

  /// <summary>
  /// Сохраняет оба представления протокола и показывает панель управления сохранёнными файлами.
  /// </summary>
  /// <param name="settings">Настройки завершённого действия.</param>
  /// <param name="protocol">Компонент отображения и хранения протокола.</param>
  public async Task SaveAndExposeAsync(ActionSettings settings, ProtocolUI protocol)
  {
    await protocol.SaveProtocolAsync(protocol.Header);
    await protocol.SaveInspectionProtocolAsync(protocol.Header, settings.CheckType);
    protocol.ShowProtocolManager();
  }
}
