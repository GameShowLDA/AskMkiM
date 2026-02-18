using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost;
using Message;
using System.IO;
using System.Windows;
using UI.Controls.TextEditor;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services.ProtocolManager
{
  /// <summary>
  /// Сервис управления протоколами испытаний.
  /// 
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Формирование текстового представления протокола с ошибками или без них.</item>
  ///   <item>Отображение протокола в интерфейсе программы или экспорт в PDF-файл.</item>
  ///   <item>Создание и открытие вкладки редактора с протоколом.</item>
  /// </list>
  /// </summary>
  public class ProtocolService : IProtocolViewerService
  {
    private readonly Components.MultiEditorMethods.FileManager _fileManager;

    /// <summary>
    /// Инициализирует новый экземпляр сервиса управления протоколами.
    /// </summary>
    /// <param name="fileManager">Главный файловый менеджер, необходимый для работы с контейнерами и редакторами.</param>
    public ProtocolService(Components.MultiEditorMethods.FileManager fileManager)
    {
      _fileManager = fileManager;
    }

    /// <summary>
    /// Отображает протокол проверки, сформированный по данным <see cref="ProtocolModel"/>.
    /// </summary>
    /// <param name="protocol">Модель протокола.</param>
    /// <param name="showInSoftware">Если <c>true</c> — открыть в редакторе, иначе — экспортировать в PDF.</param>
    public void ViewProtocol(ProtocolModel protocol, bool showInSoftware)
    {
      string protocolText = BuildProtocolText(protocol);
      if (string.IsNullOrEmpty(protocolText))
        return;

      if (showInSoftware)
        OpenProtocolInEditor(protocol, protocolText);
      else
        ExportProtocolAsPdf(protocol.ProgramName, protocolText);
      PrintUtility.PrintProtocol(protocol, protocolText);
    }

    #region 📄 Формирование текста протокола

    /// <summary>
    /// Формирует текст протокола с ошибками или без них.
    /// </summary>
    private string BuildProtocolText(ProtocolModel protocol)
    {
      return protocol.Errors.Count > 0
          ? ProtocolModel.GetProtocolWithErrorsText(protocol)
          : ProtocolModel.GetProtocolText(protocol);
    }

    #endregion

    #region 📤 Экспорт в PDF

    /// <summary>
    /// Сохраняет протокол в формате PDF и открывает его в стандартном приложении.
    /// </summary>
    private void ExportProtocolAsPdf(string programName, string protocolText)
    {
      var generator = new PdfProtocolGenerator();
      generator.GenerateAndSavePdfProtocol(programName, protocolText);
    }

    #endregion

    #region 📝 Открытие в редакторе

    /// <summary>
    /// Открывает протокол внутри интерфейса программы в виде вкладки текстового редактора.
    /// </summary>
    private void OpenProtocolInEditor(ProtocolModel protocol, string protocolText)
    {
      Application.Current.Dispatcher.BeginInvoke(new Action(() =>
      {
        try
        {
          var container = GetOrCreateProtocolContainer();
          string newFileName = BuildProtocolFileName(protocol);
          string newFilePath = BuildProtocolFilePath(protocol, newFileName);

          var textEditor = CreateReadOnlyProtocolEditor(newFilePath, protocolText);

          ShowProtocolInEditor(container, newFileName, textEditor);
        }
        catch (Exception ex)
        {
          ShowEditorError(ex);
        }
      }));
    }

    /// <summary>
    /// Получает или создаёт контейнер для протоколов.
    /// </summary>
    private TextEditorContainer GetOrCreateProtocolContainer()
    {
      var container = _fileManager.ContainerService.GetEditorContainer(EditorType.Protocol);
      if (container != null && container.DockManager.DockItems.Count > 0)
      {
        _fileManager.ContainerService.RemoveEditorContainer(container, EditorType.Protocol);
      }
      return _fileManager.ContainerService.CreateEditorContainer(EditorType.Protocol);
    }

    /// <summary>
    /// Формирует имя файла протокола.
    /// </summary>
    private string BuildProtocolFileName(ProtocolModel protocol)
    {
      return $"{Path.GetFileNameWithoutExtension(protocol.ProgramName)} от {DateTime.Now:dd-MM-yyyy HH-mm-ss}.lstw";
    }

    /// <summary>
    /// Формирует путь сохранения протокола.
    /// </summary>
    private string BuildProtocolFilePath(ProtocolModel protocol, string newFileName)
    {
      var directory = Path.GetDirectoryName(protocol.ProgramPath) ?? string.Empty;
      return Path.Combine(directory, newFileName);
    }

    /// <summary>
    /// Создаёт экземпляр текстового редактора с протоколом в режиме только для чтения.
    /// </summary>
    private TextEditorUI CreateReadOnlyProtocolEditor(string filePath, string protocolText)
    {
      var textEditorModel = new TextEditorModel(filePath);
      var textEditor = _fileManager.TextEditorService.CreateTextEditor(textEditorModel, protocolText, FileType.Protocol);
      textEditor.IsReadOnly = true;
      EditorEventAdapter.RaiseTextEditorActivated(textEditor);
      return textEditor;
    }

    /// <summary>
    /// Отображает протокол в редакторе.
    /// </summary>
    private void ShowProtocolInEditor(TextEditorContainer container, string fileName, TextEditorUI editor)
    {
      _fileManager.DockItemService.ShowEditorDockItem(fileName, container, editor, EditorType.Protocol);
      _fileManager.ControlManagerService.ShowEditorContainer(container, EditorType.Protocol);
    }

    /// <summary>
    /// Отображает сообщение об ошибке при открытии протокола.
    /// </summary>
    private void ShowEditorError(Exception ex)
    {
      MessageBoxCustom.Show($"Ошибка при открытии протокола: {ex.Message}", "Ошибка", image: MessageBoxImage.Error);
      LogException("Ошибка при открытии протокола", ex);
    }

    #endregion
  }
}
