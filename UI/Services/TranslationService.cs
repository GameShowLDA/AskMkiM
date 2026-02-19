using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Message;
using System.Windows;
using UI.Components.MultiEditorMethods;
using UI.Controls;
using UI.Controls.TextEditor;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services
{
  /// <summary>
  /// Сервис управления процессом трансляции файлов и их отображением в редакторе.
  /// 
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Создание редактора с результатом трансляции.</item>
  ///   <item>Добавление и отображение вкладок транслятора (<see cref="TranslatorItem"/>).</item>
  ///   <item>Удаление вкладок транслятора и освобождение ресурсов контейнера.</item>
  /// </list>
  /// </summary>
  public class TranslationService : ITranslationService
  {
    private readonly UI.Components.MultiEditorMethods.FileManager _fileManager;

    /// <summary>
    /// Инициализирует новый экземпляр сервиса трансляции.
    /// </summary>
    /// <param name="fileManager">Главный файловый менеджер, обеспечивающий доступ к контейнерам и редакторам.</param>
    public TranslationService(UI.Components.MultiEditorMethods.FileManager fileManager)
    {
      _fileManager = fileManager;
    }

    /// <summary>
    /// Создаёт новый текстовый редактор для отображения результатов трансляции.
    /// </summary>
    /// <returns>Экземпляр <see cref="TextEditorUI"/> с предзаполненным сообщением и режимом только для чтения
    public ITextEditorView CreateTranslationFile(string parentFilePath)
    {
      string fileName = $"Трансляция_{DateTime.Now:HHmmss}.opkw";
      var textEditorModel = new TextEditorModel(parentFilePath, fileName);

      var textEditor = new TextEditorUI(FileType.OPKW, textEditorModel)
      {
        Text = "// Результат трансляции появится здесь...",
        IsReadOnly = true
      };

      return textEditor;
    }

    /// <summary>
    /// Добавляет вкладку транслятора в редактор, объединяя исходный файл и результат трансляции.
    /// </summary>
    /// <param name="sourceEditor">Редактор с исходным файлом.</param>
    /// <param name="translatedEditor">Редактор с результатом трансляции.</param>
    /// <param name="editorType">Тип контейнера для размещения вкладки.</param>
    /// <returns>Экземпляр <see cref="TranslatorItem"/>, отображающий оба редактора.</returns>
    public async Task<TranslatorItem> AddTranslatorItem(ITextEditorView editor, ITextEditorView translateEditor, EditorType editorType)
    {
      // TODO: добавить событие на закрытие вкладок
      try
      {
        TextEditorContainer textEditorContainer = _fileManager.ContainerService.GetEditorContainer(editorType);
        if (textEditorContainer == null)
        {
          textEditorContainer = _fileManager.ContainerService.CreateEditorContainer(editorType);
        }
        else
        {
          if (textEditorContainer.DockManager.DockItems.Count > 0)
          {
            _fileManager.ContainerService.RemoveEditorContainer(textEditorContainer, editorType);
            textEditorContainer = _fileManager.ContainerService.CreateEditorContainer(editorType);
          }
        }

        TextEditorContainer protocolContainer = _fileManager.ContainerService.GetEditorContainer(EditorType.Protocol);
        if (protocolContainer != null && protocolContainer.DockManager.DockItems.Count > 0)
        {
          _fileManager.ContainerService.RemoveEditorContainer(protocolContainer, EditorType.Protocol);
        }

        TextEditorContainer executorContainer = _fileManager.ContainerService.GetEditorContainer(EditorType.Run);
        if (executorContainer != null && executorContainer.DockManager.DockItems.Count > 0)
        {
          _fileManager.ContainerService.RemoveEditorContainer(executorContainer, EditorType.Run);

        }

        var item = await _fileManager.DockItemService.ShowTranslatorDockItemAsync($"Трансляция {editor.TextEditorModel.FileName}", textEditorContainer, editor, translateEditor);

        _fileManager.ControlManagerService.ShowEditorContainer(textEditorContainer, EditorType.Translator);
        return item;
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", image: MessageBoxImage.Error);
        LogException($"Ошибка при чтении файла", ex);
        return null;
      }
    }

    /// <summary>
    /// Удаляет вкладку транслятора из контейнера и освобождает ресурсы, если контейнер пуст.
    /// </summary>
    /// <param name="translatorItem">Экземпляр <see cref="TranslatorItem"/>, который необходимо удалить.</param>
    /// <param name="editorType">Тип контейнера, из которого удаляется элемент.</param>
    public async Task RemoveTranslatorTabAsync(TranslatorItem translatorItem, EditorType editorType)
    {
      try
      {
        TextEditorContainer textEditorContainer = _fileManager.ContainerService.GetEditorContainer(editorType);
        if (textEditorContainer == null)
        {
          return;
        }
        var controlManager = new ControlManager(_fileManager.EditorWorkspaceModel);

        var foundPage = _fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Text == EditorType.Translator.ToString());
        controlManager.RemoveControl(foundPage, translatorItem);

        //textEditorContainer.RemoveTranslatorItem(translatorItem);
        if (textEditorContainer.DockManager.DockItems.Count == 0)
        {
          _fileManager.ContainerService.RemoveEditorContainer(textEditorContainer, EditorType.Translator);
        }
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", image: MessageBoxImage.Error);
        LogException($"Ошибка при чтении файла", ex);
        return;
      }
    }
  }
}
