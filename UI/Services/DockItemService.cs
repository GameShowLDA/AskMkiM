using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Metadata.Static;
using Message;
using System.Windows;
using System.Windows.Controls;
using UI.Components.FileComparerControls;
using UI.Components.MultiEditorMethods;
using UI.Controls;
using UI.Controls.Runner;
using UI.Controls.TextEditor;
using UI.Windows.WpfDocking.Windows.Docking;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services
{
  /// <summary>
  /// Сервис управления вкладками (DockItem) внутри контейнеров редактора.
  /// 
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Создание и отображение новых вкладок редактора.</item>
  ///   <item>Открытие парных редакторов для трансляции.</item>
  ///   <item>Обработка логики закрытия вкладок и удаления контейнеров.</item>
  ///   <item>Регистрация событий открытия и закрытия редакторов.</item>
  /// </list>
  /// </summary>
  public class DockItemService
  {

    private readonly UI.Components.MultiEditorMethods.FileManager _fileManager;

    /// <summary>
    /// Создаёт новый экземпляр сервиса управления вкладками редактора.
    /// </summary>
    /// <param name="fileManager">Главный файловый менеджер для работы с контейнерами и редакторами.</param>
    public DockItemService(UI.Components.MultiEditorMethods.FileManager fileManager)
    {
      _fileManager = fileManager;
    }

    /// <summary>
    /// Отображает указанную вкладку <see cref="DockItem"/> в переданном контейнере редактора.
    /// Если контейнер ещё не загружен — вкладка будет показана после его инициализации.
    /// </summary>
    /// <param name="container">Контейнер редактора, в котором требуется отобразить вкладку.</param>
    /// <param name="dockItem">Вкладка для отображения.</param>
    public void ShowDockItem(TextEditorContainer textEditorContainer, DockItem dockItem)
    {
      try
      {
        var dockControl = textEditorContainer?.DockManager;

        if (dockControl == null)
        {
          LogError("DockControl не найден (null). Невозможно отобразить вкладку.");
          return;
        }

        LogInformation($"Попытка показать DockItem. Title: {dockItem.Title}, IsLoaded: {dockControl.IsLoaded}, DockItems.Count: {dockControl.DockItems.Count}");

        if (!dockControl.IsLoaded)
        {
          LogWarning("DockControl ещё не загружен. Подписка на Loaded...");

          var capturedDockItem = dockItem;
          dockControl.Loaded += (s, e) =>
          {
            try
            {
              LogInformation("DockControl загрузился. Показываем вкладку.");
              LogInformation("DockItem отображён после загрузки.");
              var isControlProgramActive = false;
              if (dockItem.Title.Contains(".pk") || dockItem.Title.Contains(".opk") || dockItem.Content is RunControl || dockItem.Content is TranslatorItem)
              {
                isControlProgramActive = true;
              }

              SystemStateManager.SetIsControlProgramActive(isControlProgramActive).ConfigureAwait(true);
              capturedDockItem.Show(dockControl, DockPosition.Document);
            }
            catch (Exception ex)
            {
              LogException("Ошибка при отображении DockItem после загрузки:", ex);
            }
          };
        }
        else
        {
          var isControlProgramActive = false;
          if (dockItem.Title.Contains(".pk") || dockItem.Title.Contains(".opk") || dockItem.Content is RunControl || dockItem.Content is TranslatorItem)
          {
            isControlProgramActive = true;
          }

          SystemStateManager.SetIsControlProgramActive(isControlProgramActive).ConfigureAwait(true);
          dockItem.Show(dockControl, DockPosition.Document);
          LogInformation("DockItem отображён немедленно.");
        }
      }
      catch (Exception ex)
      {
        LogException("Ошибка при отображении DockItem:", ex);
      }
    }

    /// <summary>
    /// Создаёт и отображает новую вкладку-транслятор, содержащую два редактора (оригинал и результат трансляции).
    /// </summary>
    /// <param name="nameFile">Имя вкладки.</param>
    /// <param name="container">Контейнер, в котором будет отображён транслятор.</param>
    /// <param name="leftEditor">Левый редактор с исходным содержимым.</param>
    /// <param name="rightEditor">Правый редактор с результатом трансляции.</param>
    /// <returns>Экземпляр <see cref="TranslatorItem"/>, отображающий оба редактора.</returns>
    public async Task<TranslatorItem> ShowTranslatorDockItemAsync(string nameFile, TextEditorContainer textEditorContainer, TextEditorUI textEditor, TextEditorUI translatorEditor)
    {
      try
      {
        var translatorItem = new TranslatorItem();
        translatorItem.SetLeftEditor(textEditor);
        translatorItem.SetRightEditor(translatorEditor);
        translatorItem.SetRightEditorName(translatorEditor.TextEditorModel.FileName);
        translatorItem.SetLeftEditorName(textEditor.TextEditorModel.FileName);
        var dockItem = new DockItem
        {
          Title = nameFile,
          TabText = nameFile,
          Content = translatorItem
        };


        dockItem.CloseItem += (sender) =>
        {
          LogDebug($"Закрытие файла {nameFile}.");

          if (textEditorContainer != null)
          {
            var controlManager = new ControlManager(_fileManager.EditorWorkspaceModel);
            var foundPage = _fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Text == EditorType.Translator.ToString());
            controlManager.RemoveControl(foundPage, translatorItem).ConfigureAwait(true);
            _fileManager.EditorWorkspaceModel.FilePaths.Remove(dockItem.TabText);
            if (textEditorContainer != null && textEditorContainer.DockManager.DockItems.Count(item => item.DockPosition != DockPosition.Hidden) == 0)
            {
              LogDebug($"Закрытие контейнера типа \"{EditorType.Translator.ToString()}\".");
              _fileManager.ContainerService.RemoveEditorContainer(textEditorContainer, EditorType.Translator);
            }

            EditorEventAdapter.RaiseTextEditorContainerClosing(true, nameFile);
          }
        };

        await Task.Delay(1).ConfigureAwait(true);

        ShowDockItem(textEditorContainer, dockItem);

        return translatorItem;
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Системная ошибка: {ex}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError($"Системная ошибка: {ex}");
        return null;
      }
    }


    /// <summary>
    /// Создаёт и отображает новую вкладку редактора.  
    /// При необходимости обрабатывает повторное открытие, назначает события и задаёт режимы.
    /// </summary>
    /// <param name="nameFile">Имя вкладки.</param>
    /// <param name="textEditorContainer">Контейнер редактора.</param>
    /// <param name="textEditor">Содержимое вкладки (например, редактор, архив, панель сравнения).</param>
    /// <param name="editorType">Тип редактора (по умолчанию — текстовый редактор).</param>
    internal async void ShowEditorDockItem(string nameFile, TextEditorContainer textEditorContainer, UserControl textEditor, EditorType editorType = null)
    {
      LogDebug($"Создание DockItem для файла {nameFile}");
      var dockItem = new DockItem
      {
        Title = nameFile,
        TabText = nameFile,
        Content = textEditor
      };

      if ((dockItem.Content is TextEditorUI && editorType == EditorType.Archive) || (dockItem.Content is RunControl && editorType == EditorType.Run))
      {
        InitializeWithoutSave(dockItem, editorType);
      }
      else if (dockItem.Content is TextEditorUI || dockItem.Content is FileCompareControl)
      {
        if (editorType != EditorType.Protocol)
        {
          editorType = EditorType.TextEditor;
        }
        else
        {
          (dockItem.Content as TextEditorUI).IsReadOnly = true;
        }

        InitializeWithSave(nameFile, textEditorContainer, textEditor, editorType, dockItem);
      }

      await Task.Delay(1).ConfigureAwait(true);

      ShowDockItem(textEditorContainer, dockItem);
      _fileManager.ControlManagerService.ShowEditorContainer(textEditorContainer, editorType);
    }

    /// <summary>
    /// Настраивает DockItem, который не требует сохранения состояния.
    /// </summary>
    private EditorType InitializeWithoutSave(DockItem dockItem, EditorType editorType)
    {
      dockItem.ItemClosed += (sender) =>
      {
        var controlManager = new ControlManager(_fileManager.EditorWorkspaceModel);
        var foundPage = _fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Text == editorType.ToString());
        TextEditorContainer translatorContainer = _fileManager.ContainerService.GetEditorContainer(editorType);

        if (translatorContainer != null && translatorContainer.DockManager.DockItems.Count(item => item.DockPosition != DockPosition.Hidden) == 0)
        {
          _fileManager.ContainerService.RemoveEditorContainer(translatorContainer, editorType);
        }
        SystemStateManager.SetIsControlProgramActive(false).ConfigureAwait(true);
      };
      return editorType;
    }

    /// <summary>
    /// Настраивает DockItem, который требует сохранения состояния (например, файл, открытый в редакторе).
    /// </summary>
    internal void InitializeWithSave(string nameFile, TextEditorContainer textEditorContainer, UserControl textEditor, EditorType editorType, DockItem dockItem)
    {
      LogDebug($"Тип редактора для файла {nameFile}: {editorType.ToString()}");

      dockItem.CloseItem += (sender) =>
      {
        LogDebug($"Закрытие файла {nameFile}.");

        if (textEditorContainer != null && editorType != null)
        {
          var controlManager = new ControlManager(_fileManager.EditorWorkspaceModel);
          var foundPage = _fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Text == editorType.ToString());
          controlManager.RemoveControl(foundPage, textEditor).ConfigureAwait(true);
          _fileManager.EditorWorkspaceModel.FilePaths.Remove(dockItem.TabText);
          if (_fileManager.EditorWorkspaceModel.FilePaths.Count == 0)
          {
            LogDebug($"Закрытие контейнера типа \"{editorType.ToString()}\".");
            _fileManager.ContainerService.RemoveEditorContainer(textEditorContainer, editorType);
          }

          EditorEventAdapter.RaiseTextEditorContainerClosing(true, nameFile);
          SystemStateManager.SetIsControlProgramActive(false).ConfigureAwait(true);
        }
      };
    }
  }
}
